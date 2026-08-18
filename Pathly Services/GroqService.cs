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
                GroqPromptBuilder.BuildUserPrompt(academicRecord, apsResult, careerEvidence, psychometricProfile));

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

            var responseBody = await CallGroqAsync(
                GroqPromptBuilder.BuildDocumentExtractionSystemPrompt(),
                GroqPromptBuilder.BuildDocumentExtractionUserPrompt(rawText));

            return ParseExtractionResponse(responseBody);
        }

        private async Task<string> CallGroqAsync(string systemPrompt, string userPrompt)
        {
            var requestBody = new
            {
                model = _GroqSettings.Model,
                max_tokens = 8000,
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
