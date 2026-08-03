using System;
using System.Collections.Generic;
using System.Text;

namespace Pathly_Services
{
    using Microsoft.Extensions.Options;
    using Pathly_Core;
    using Pathly_Core.Pathly_Core;
    using Pathly_DTOs;
    using Pathly_Helper;
    using PathlyInterfaces.IService;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Text.Json;

    namespace Pathly_Services
    {
        public class AzureModelRouterService : IGroqService
        {
            private readonly HttpClient _HttpClient;
            private readonly AzureFoundrySettings _FoundrySettings;

            public AzureModelRouterService(HttpClient httpClient,
                                           IOptions<AzureFoundrySettings> foundrySettings)
            {
                _HttpClient = httpClient;
                _FoundrySettings = foundrySettings.Value;
            }

            public async Task<AiResponseDto> AnalyzeAcademicRecordAsync(ExtractedAcademicRecordDto academicRecord, ApsResultDto apsResult)
            {
                var requestBody = new
                {
                    model = _FoundrySettings.DeploymentName,

                    max_completion_tokens = 8000,
                    messages = new[]
                    {
                    new
                    {
                        role = "system",
                        content = GroqPromptBuilder.BuildSystemPrompt()
                    },
                    new
                    {
                        role = "user",
                        content = GroqPromptBuilder.BuildUserPrompt(academicRecord, apsResult)
                    }
                }
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var requestUri = $"{_FoundrySettings.Endpoint.TrimEnd('/')}/openai/deployments/{_FoundrySettings.DeploymentName}/chat/completions?api-version=2024-02-01";
                using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
                {
                    Content = content
                };

                request.Headers.Add("api-key", _FoundrySettings.ApiKey?.Trim());

                var response = await _HttpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();

                    throw new HttpRequestException($"Azure Model Router call failed: {response.StatusCode} - {error}");
                }

                var responseBody = await response.Content.ReadAsStringAsync();

                Console.WriteLine(responseBody);

                return ParseRouterResponse(responseBody);
            }

            private static string SanitizeJson(string json)
            {
                var sb = new StringBuilder(json.Length);
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
                        if (c == '\n') { sb.Append("\\n"); continue; }
                        if (c == '\r') { sb.Append("\\r"); continue; }
                        if (c == '\t') { sb.Append("\\t"); continue; }
                    }

                    sb.Append(c);
                }

                return sb.ToString();
            }

            private AiResponseDto ParseRouterResponse(string responseBody)
            {
                using var doc = JsonDocument.Parse(responseBody);

                var finishReason = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("finish_reason")
                    .GetString();

                Console.WriteLine($"Finish Reason: {finishReason}");

                if (doc.RootElement.TryGetProperty("model", out var modelUsed))
                {
                    Console.WriteLine($"Model Router selected: {modelUsed.GetString()}");
                }

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
                        $"Azure Model Router returned a truncated response. " +
                        $"The JSON was cut off before completion. " +
                        $"Response length: {cleaned.Length} characters. " +
                        $"Consider increasing max_completion_tokens.");
                }

                var result = JsonSerializer.Deserialize<AiResponseDto>(
                    cleaned,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        Converters = { new StringToListConverter(), new ArrayToStringConverter(), new CareerMatchListConverter() }
                    });

                return result ?? throw new InvalidOperationException(
                    "Failed to deserialize Azure Model Router response.");
            }
        }
    }
}
