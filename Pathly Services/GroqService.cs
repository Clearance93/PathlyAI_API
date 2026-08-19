using Microsoft.Extensions.Options;
using Pathly_Core;
using Pathly_DTOs;
using Pathly_Helper;
using PathlyInterfaces.IService;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Pathly_Services
{
    public class GroqService : IGroqService, IPrimaryCareerAiProvider, IDocumentStructuringService
    {
        private readonly HttpClient _HttpClient;
        private readonly GroqSettings _GroqSettings;

        public GroqService(HttpClient httpClient,
                          IOptions<GroqSettings> groqSettings)
        {
            _HttpClient = httpClient;
            _GroqSettings = groqSettings.Value;
        }

        public async Task<AiResponseDto> AnalyzeAcademicRecordAsync(ExtractedAcademicRecordDto academicRecord, ApsResultDto apsResult)
        {
            return await AnalyzeAcademicRecordAsync(academicRecord, apsResult, null, null);
        }

        public async Task<AiResponseDto> AnalyzeAcademicRecordAsync(
            ExtractedAcademicRecordDto academicRecord,
            ApsResultDto apsResult,
            List<CareerEvidenceDto>? careerEvidence,
            PsychometricProfileDto? psychometricProfile)
        {
            var responseBody = await CallGroqAsync(
                GroqPromptBuilder.BuildSystemPrompt(),
                GroqPromptBuilder.BuildUserPrompt(academicRecord, apsResult, careerEvidence, psychometricProfile),
                maxTokens: 8000);

            return ParseGroqResponse(responseBody);
        }

        /// <summary>
        /// Free replacement for Azure Document Intelligence's layout/field parsing. Takes raw text
        /// already pulled from the file (via PdfPig or Tesseract OCR — see DocumentExtractionService)
        /// and asks Groq to reason it into a structured academic record. No Subjects/RawExtractedText
        /// bookkeeping happens here — that's the caller's responsibility.
        /// </summary>
        public async Task<ExtractedAcademicRecordDto> StructureAcademicRecordAsync(string rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText))
            {
                return new ExtractedAcademicRecordDto { RawExtractedText = rawText };
            }

            var systemPrompt = GroqPromptBuilder.BuildDocumentExtractionSystemPrompt();
            var userPrompt = GroqPromptBuilder.BuildDocumentExtractionUserPrompt(rawText);

            var responseBody = await CallGroqAsync(systemPrompt, userPrompt, EstimateExtractionMaxTokens(systemPrompt, userPrompt));

            return ParseExtractionResponse(responseBody);
        }

        // Groq's free/on_demand tier enforces a tokens-per-minute cap covering prompt +
        // requested completion combined (8000 TPM at time of writing for openai/gpt-oss-120b). A
        // flat completion budget either wastes headroom on short transcripts or risks truncating
        // long ones (a 60+ module university transcript can genuinely need 2500+ completion
        // tokens of JSON), so this scales the request instead:
        //   1. Roughly estimate prompt tokens (~4 characters per token is a standard approximation
        //      for English text and holds up fine for a size estimate, not exact billing).
        //   2. Leave a safety margin so we don't shave the request right up to the TPM ceiling.
        //   3. Clamp between a floor (small records still get enough room) and a ceiling (very
        //      large documents can't just take an unbounded completion budget — see the
        //      NeedsManualReview + rechunking note in DocumentExtractionService for that case).
        private const int TokensPerMinuteBudget = 8000;
        private const int SafetyMarginTokens = 300;
        private const int MinExtractionTokens = 1200;
        private const int MaxExtractionTokens = 4000;

        private static int EstimateExtractionMaxTokens(string systemPrompt, string userPrompt)
        {
            var estimatedPromptTokens = (systemPrompt.Length + userPrompt.Length) / 4;
            var available = TokensPerMinuteBudget - estimatedPromptTokens - SafetyMarginTokens;

            return Math.Clamp(available, MinExtractionTokens, MaxExtractionTokens);
        }

        private async Task<string> CallGroqAsync(string systemPrompt, string userPrompt, int maxTokens)
        {
            var requestBody = new
            {
                model = _GroqSettings.Model,
                max_tokens = maxTokens,
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = systemPrompt
                    },
                    new
                    {
                        role = "user",
                        content = userPrompt
                    }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _GroqSettings.GroqApiKey);

            var response = await _HttpClient.PostAsync(_GroqSettings.BaseUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();

                throw new HttpRequestException($"Groq API failed: {response.StatusCode} - {error}");
            }

            var responseBody = await response.Content.ReadAsStringAsync();

            Console.WriteLine(responseBody);

            return responseBody;
        }

        private static string SanitizeJson(string json)
        {
            var sb = new System.Text.StringBuilder(json.Length);
            bool inString = false;
            bool escaped = false;

            for (int i = 0; i < json.Length; i++)
            {
                char c = json[i];

                if (escaped)
                {
                    sb.Append(c);
                    escaped = false;
                    continue;
                }

                if (c == '\\')
                {
                    escaped = true;
                    sb.Append(c);
                    continue;
                }

                if (c == '"')
                {
                    inString = !inString;
                    sb.Append(c);
                    continue;
                }

                if (inString)
                {
                    // Replace unescaped control characters inside strings
                    if (c == '\n') { sb.Append("\\n"); continue; }
                    if (c == '\r') { sb.Append("\\r"); continue; }
                    if (c == '\t') { sb.Append("\\t"); continue; }
                }

                sb.Append(c);
            }

            return sb.ToString();
        }

        private static string ExtractCleanedJsonContent(string responseBody)
        {
            using var doc = JsonDocument.Parse(responseBody);

            var finishReason = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("finish_reason")
                .GetString();

            Console.WriteLine($"Finish Reason: {finishReason}");

            var messageContent = doc
                .RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? string.Empty;

            var cleaned = messageContent
                .Replace("```json", "")
                .Replace("```", "")
                .Trim();

            cleaned = SanitizeJson(cleaned);

            if (!cleaned.TrimEnd().EndsWith("}"))
            {
                throw new InvalidOperationException(
                    $"Groq returned a truncated response. " +
                    $"The JSON was cut off before completion. " +
                    $"Response length: {cleaned.Length} characters. " +
                    $"Consider increasing max_tokens.");
            }

            return cleaned;
        }

        private AiResponseDto ParseGroqResponse(string responseBody)
        {
            var cleaned = ExtractCleanedJsonContent(responseBody);

            var result = JsonSerializer.Deserialize<AiResponseDto>(
                cleaned,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new StringToListConverter(), new ArrayToStringConverter(), new CareerMatchListConverter() }
                });

            return result ?? throw new InvalidOperationException(
                "Failed to deserialize Groq response.");
        }

        private ExtractedAcademicRecordDto ParseExtractionResponse(string responseBody)
        {
            var cleaned = ExtractCleanedJsonContent(responseBody);

            var parsed = JsonSerializer.Deserialize<GroqExtractionResponse>(
                cleaned,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (parsed is null)
            {
                throw new InvalidOperationException("Failed to deserialize Groq extraction response.");
            }

            return new ExtractedAcademicRecordDto
            {
                StudentName = NullIfEmpty(parsed.StudentName),
                InstitutionName = NullIfEmpty(parsed.InstitutionName),
                InstitutionType = string.IsNullOrWhiteSpace(parsed.InstitutionType) ? "Unknown" : parsed.InstitutionType,
                StudyLevel = NullIfEmpty(parsed.StudyLevel),
                AcademicPeriod = NullIfEmpty(parsed.AcademicPeriod),
                Subjects = (parsed.Subjects ?? new List<GroqExtractedSubject>())
                    .Where(s => !string.IsNullOrWhiteSpace(s.SubjectName))
                    .Select(s => new ExtractedSubjectDto
                    {
                        ExtractionSubjectId = Guid.NewGuid(),
                        SubjectName = s.SubjectName!.Trim(),
                        RawMark = NullIfEmpty(s.RawMark),
                        NumericMark = s.NumericMark,
                        Symbol = NullIfEmpty(s.Symbol),
                        MarkType = string.IsNullOrWhiteSpace(s.MarkType) ? "Symbol" : s.MarkType
                    })
                    .ToList()
            };
        }

        private static string? NullIfEmpty(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        /// <summary>
        /// Mirrors the shape of the JSON schema requested in
        /// <see cref="GroqPromptBuilder.BuildDocumentExtractionUserPrompt"/>. Kept private/internal —
        /// this is purely a deserialization shim, never exposed outside GroqService.
        /// </summary>
        private class GroqExtractionResponse
        {
            public string? StudentName { get; set; }
            public string? InstitutionName { get; set; }
            public string? InstitutionType { get; set; }
            public string? StudyLevel { get; set; }
            public string? AcademicPeriod { get; set; }
            public List<GroqExtractedSubject>? Subjects { get; set; }
        }

        private class GroqExtractedSubject
        {
            public string? SubjectName { get; set; }
            public string? RawMark { get; set; }
            public int? NumericMark { get; set; }
            public string? Symbol { get; set; }
            public string? MarkType { get; set; }
        }
    }
}
