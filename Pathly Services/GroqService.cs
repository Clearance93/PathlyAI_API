using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pathly_Core;
using Pathly_DTOs;
using Pathly_Helper;
using PathlyInterfaces.IService;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Pathly_Services
{
    public class GroqService : IGroqService, IPrimaryCareerAiProvider, IDocumentStructuringService
    {
        private readonly HttpClient _HttpClient;
        private readonly GroqSettings _GroqSettings;
        private readonly ILogger<GroqService> _Logger;
        private readonly List<GroqKeySettings> _AllKeys;

        // Rotated on every request so consecutive calls start on different keys — this spreads
        // each key's tokens-per-minute budget across the whole key pool instead of always
        // hammering key 1 until it 429s and only then falling back.
        private static int _KeyRotationIndex;

        public GroqService(HttpClient httpClient,
                          IOptions<GroqSettings> groqSettings,
                          IConfiguration configuration,
                          ILogger<GroqService> logger)
        {
            _HttpClient = httpClient;
            _GroqSettings = groqSettings.Value;
            _Logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Fallback keys can be stored as flat top-level sections (e.g. user secrets using
            // "EduHubGroq:GroqApiKey" / "PahtlyGroq:GroqApiKey" instead of the "Groq:FallbackKeys"
            // array). Read them straight from configuration so a dead primary key doesn't take the
            // whole pipeline down with it.
            var flatKeys = new List<GroqKeySettings>();
            var flatConfig = new[]
            {
                ("EduHubGroq", configuration["EduHubGroq:GroqApiKey"]),
                ("PahtlyGroq", configuration["PahtlyGroq:GroqApiKey"])
            };

            foreach (var (name, key) in flatConfig)
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                flatKeys.Add(new GroqKeySettings
                {
                    Name = name,
                    GroqApiKey = key,
                    BaseUrl = _GroqSettings.BaseUrl,
                    Model = _GroqSettings.Model
                });
            }

            _AllKeys = BuildKeyList(_GroqSettings, flatKeys);
        }

        private static List<GroqKeySettings> BuildKeyList(GroqSettings settings, List<GroqKeySettings>? flatFallbacks = null)
        {
            var keys = new List<GroqKeySettings>();

            if (!string.IsNullOrWhiteSpace(settings.GroqApiKey))
            {
                keys.Add(new GroqKeySettings
                {
                    Name = "Primary",
                    GroqApiKey = settings.GroqApiKey,
                    BaseUrl = settings.BaseUrl,
                    Model = settings.Model
                });
            }

            keys.AddRange(settings.FallbackKeys);

            // Flat fallback sections (e.g. user secrets using "EduHubGroq:GroqApiKey" instead of
            // the "Groq:FallbackKeys" array). Merged here so a dead primary key doesn't take the
            // whole pipeline down with it.
            foreach (var flat in flatFallbacks ?? new List<GroqKeySettings>())
            {
                if (string.IsNullOrWhiteSpace(flat.GroqApiKey))
                {
                    continue;
                }

                if (keys.Any(k => k.Name == flat.Name || k.GroqApiKey == flat.GroqApiKey))
                {
                    continue;
                }

                keys.Add(new GroqKeySettings
                {
                    Name = flat.Name,
                    GroqApiKey = flat.GroqApiKey,
                    BaseUrl = string.IsNullOrWhiteSpace(flat.BaseUrl) ? settings.BaseUrl : flat.BaseUrl,
                    Model = string.IsNullOrWhiteSpace(flat.Model) ? settings.Model : flat.Model
                });
            }

            return keys;
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
            var systemPrompt = GroqPromptBuilder.BuildSystemPrompt();
            var userPrompt = GroqPromptBuilder.BuildUserPrompt(academicRecord, apsResult, careerEvidence, psychometricProfile);

            // Analysis output (feedback, roadmap, career matches, study tips, etc.) is much longer
            // than an extraction JSON, so it gets a higher floor/ceiling than extraction — but it's
            // still computed from actual prompt size instead of a flat 8000, which is what caused
            // the 413 here: a flat request guarantees overage on the TPM cap the moment the prompt
            // (academic record + APS + career evidence + psychometric profile) has any real size.
            var maxTokens = EstimateMaxTokens(systemPrompt, userPrompt, floorTokens: 800, ceilingTokens: 6500);

            return await CallGroqAsync(systemPrompt, userPrompt, maxTokens, ParseGroqResponse);
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

            // Extraction JSON is small even for a long transcript — DocumentExtractionService also
            // caps input text upstream, so 1200 as a floor is safe here (see the "never force above
            // available" note on AnalyzeAcademicRecordAsync above for why that floor is lower there).
            var maxTokens = EstimateMaxTokens(systemPrompt, userPrompt, floorTokens: 1200, ceilingTokens: 4000);

            return await CallGroqAsync(systemPrompt, userPrompt, maxTokens, ParseExtractionResponse);
        }

        // Groq's free/on_demand tier enforces a tokens-per-minute cap covering prompt +
        // requested completion combined (8000 TPM at time of writing for openai/gpt-oss-120b). A
        // flat completion budget either wastes headroom on short requests or guarantees overage on
        // larger ones (exactly what caused the 413 seen against AnalyzeAcademicRecordAsync — its
        // max_tokens was flatly 8000 regardless of how big the prompt already was), so both calls
        // scale their request off actual prompt size instead:
        //   1. Roughly estimate prompt tokens (~4 characters per token is a standard approximation
        //      for English text and holds up fine for a size estimate, not exact billing).
        //   2. Leave a safety margin so we don't shave the request right up to the TPM ceiling.
        //   3. Clamp to a floor/ceiling appropriate to the call. NOTE: if the prompt itself is
        //      already large enough that "available" falls below the floor, the floor wins and the
        //      request can still exceed budget — that's an intentional trade-off (a too-short
        //      completion is recoverable via the truncation check below + retry; a request that's
        //      too small to be useful isn't worth sending at all). Callers with no upstream cap on
        //      prompt size (AnalyzeAcademicRecordAsync) use a lower floor for exactly this reason;
        //      StructureAcademicRecordAsync can safely use a higher floor because
        //      DocumentExtractionService already caps its input text.
        private const int TokensPerMinuteBudget = 8000;
        private const int SafetyMarginTokens = 300;

        private static int EstimateMaxTokens(string systemPrompt, string userPrompt, int floorTokens, int ceilingTokens)
        {
            var estimatedPromptTokens = (systemPrompt.Length + userPrompt.Length) / 4;
            var available = TokensPerMinuteBudget - estimatedPromptTokens - SafetyMarginTokens;

            return Math.Clamp(available, floorTokens, ceilingTokens);
        }

        // The free/on_demand tier also counts every call in the pipeline against one shared
        // tokens-per-minute window PER KEY. CallGroqAsync therefore works the key pool like this:
        //   1. Each request starts on a different key (round-robin, see _KeyRotationIndex) so
        //      consecutive requests spread their TPM load across keys instead of concentrating
        //      on the first one.
        //   2. On a 429 (or an unusable/truncated response) it moves to the NEXT key immediately
        //      rather than sleeping — a different key's TPM window is almost certainly still open.
        //   3. Only after a full pass over every key does it wait out the longest rate-limit delay
        //      Groq reported and try the key pool once more (skipping keys that failed with a
        //      non-retryable error such as an invalid key).
        private const int MaxKeyPoolPasses = 2;
        private const int RateLimitRetryBufferSeconds = 2;

        private async Task<TResponse> CallGroqAsync<TResponse>(
            string systemPrompt,
            string userPrompt,
            int maxTokens,
            Func<string, TResponse> parseResponse)
        {
            var keys = _AllKeys
                .Where(k => !string.IsNullOrWhiteSpace(k.GroqApiKey))
                .ToList();

            if (keys.Count == 0)
            {
                throw new HttpRequestException("No Groq API keys are configured.");
            }

            var startIndex = (int)((uint)Interlocked.Increment(ref _KeyRotationIndex) % keys.Count);
            var lastError = string.Empty;
            TimeSpan? rateLimitDelay = null;
            var sawRetryableFailure = false;
            var skippedKeys = new HashSet<string>();

            for (var pass = 0; pass < MaxKeyPoolPasses; pass++)
            {
                if (pass > 0)
                {
                    if (!sawRetryableFailure)
                    {
                        break;
                    }

                    var delay = rateLimitDelay ?? TimeSpan.FromSeconds(30);
                    _Logger.LogWarning(
                        "All {KeyCount} Groq key(s) hit their rate limit or returned unusable content — waiting {Delay:0.#}s before one more pass.",
                        keys.Count, delay.TotalSeconds);

                    await Task.Delay(delay);
                }

                for (var offset = 0; offset < keys.Count; offset++)
                {
                    var keyIndex = (startIndex + offset) % keys.Count;
                    var currentKey = keys[keyIndex];
                    var keyName = string.IsNullOrWhiteSpace(currentKey.Name) ? $"Key {keyIndex + 1}" : currentKey.Name!;

                    if (skippedKeys.Contains(keyName))
                    {
                        continue;
                    }

                    var baseUrl = string.IsNullOrWhiteSpace(currentKey.BaseUrl) ? _GroqSettings.BaseUrl : currentKey.BaseUrl;
                    var model = string.IsNullOrWhiteSpace(currentKey.Model) ? _GroqSettings.Model : currentKey.Model;

                    var requestBody = new
                    {
                        model = model,
                        max_tokens = maxTokens,
                        messages = new[]
                        {
                            new { role = "system", content = systemPrompt },
                            new { role = "user", content = userPrompt }
                        }
                    };

                    using var request = new HttpRequestMessage(HttpMethod.Post, baseUrl)
                    {
                        Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
                    };

                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", currentKey.GroqApiKey!.Trim());

                    var response = await _HttpClient.SendAsync(request);
                    var body = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        try
                        {
                            return parseResponse(body);
                        }
                        catch (Exception ex) when (ex is InvalidOperationException or JsonException)
                        {
                            // HTTP 200 but the content is truncated/unparsable — treat it like any
                            // other failure and let the next key take the work.
                            lastError = $"Groq key '{keyName}' returned unusable content: {ex.Message}";
                            sawRetryableFailure = true;
                            _Logger.LogWarning(lastError);
                            continue;
                        }
                    }

                    lastError = $"Groq API failed with key '{keyName}': {response.StatusCode} - {body}";

                    if ((int)response.StatusCode == 429)
                    {
                        var delay = GetRateLimitRetryDelay(response, body);
                        rateLimitDelay = rateLimitDelay is null || delay > rateLimitDelay ? delay : rateLimitDelay;
                        sawRetryableFailure = true;

                        _Logger.LogWarning(
                            "Groq rate limit hit on key '{KeyName}' — moving to the next key ({Offset}/{KeyCount}).",
                            keyName, offset + 1, keys.Count);

                        continue;
                    }

                    _Logger.LogWarning(
                        "Groq key '{KeyName}' failed with a non-retryable error ({StatusCode}) — skipping it for the rest of this request.",
                        keyName, response.StatusCode);

                    skippedKeys.Add(keyName);
                }
            }

            throw new HttpRequestException($"All Groq API keys exhausted. Last error: {lastError}");
        }

        private static TimeSpan GetRateLimitRetryDelay(HttpResponseMessage response, string errorBody)
        {
            if (response.Headers.RetryAfter?.Delta is { } delta && delta > TimeSpan.Zero)
            {
                return delta.Add(TimeSpan.FromSeconds(RateLimitRetryBufferSeconds));
            }

            var match = Regex.Match(
                errorBody,
                @"try again in\s*([0-9]+(?:\.[0-9]+)?)\s*s",
                RegexOptions.IgnoreCase);

            if (match.Success &&
                double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds))
            {
                return TimeSpan.FromSeconds(seconds + RateLimitRetryBufferSeconds);
            }

            return TimeSpan.FromSeconds(30);
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

        private string ExtractCleanedJsonContent(string responseBody)
        {
            using var doc = JsonDocument.Parse(responseBody);

            var finishReason = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("finish_reason")
                .GetString();

            _Logger.LogDebug("Groq finish reason: {FinishReason}", finishReason);

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
