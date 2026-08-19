using Pathly_DTOs;
using Pathly_Helper;
using PathlyInterfaces.IService;

namespace Pathly_Services
{
    /// <summary>
    /// Wraps a raw <see cref="IDocumentStructuringService"/> (Groq) with deterministic validation
    /// and retry. Groq is non-deterministic, so a re-ask genuinely can produce a better result —
    /// this retries on validation failure up to <see cref="_maxAttempts"/> times, and if it's
    /// still not clean after that, returns the best attempt with
    /// <see cref="ExtractedAcademicRecordDto.NeedsManualReview"/> set rather than throwing or
    /// silently trusting a shaky result. Mirrors the primary/fallback pattern already used by
    /// <see cref="ResilientCareerAiService"/>, just for the extraction step instead of analysis.
    /// </summary>
    public class SelfValidatingDocumentStructuringService : IDocumentStructuringService
    {
        private readonly IDocumentStructuringService _inner;
        private readonly int _maxAttempts;

        public SelfValidatingDocumentStructuringService(IDocumentStructuringService inner, int maxAttempts = 3)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));

            if (maxAttempts < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxAttempts), "Must attempt at least once.");
            }

            _maxAttempts = maxAttempts;
        }

        public async Task<ExtractedAcademicRecordDto> StructureAcademicRecordAsync(string rawText)
        {
            ExtractedAcademicRecordDto? lastAttempt = null;
            ExtractionValidationResult lastValidation = new();
            var lastFailureReason = "Unknown extraction failure.";

            for (var attempt = 1; attempt <= _maxAttempts; attempt++)
            {
                try
                {
                    lastAttempt = await _inner.StructureAcademicRecordAsync(rawText);
                }
                catch (Exception ex)
                {
                    // A thrown exception here (rate limit, truncated response, transient network
                    // failure) used to crash the whole upload on the very first attempt, with no
                    // chance to retry. Treat it the same as a failed validation instead: log it,
                    // back off briefly, and try again rather than failing the request outright.
                    lastFailureReason = ex.Message;

                    Console.WriteLine($"Extraction attempt {attempt}/{_maxAttempts} threw: {ex.Message}");

                    if (attempt < _maxAttempts)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(400 * attempt));
                    }

                    continue;
                }

                lastValidation = ExtractionValidator.Validate(rawText, lastAttempt);

                if (lastValidation.IsValid)
                {
                    lastAttempt.ExtractionWarnings = lastValidation.Warnings;
                    lastAttempt.NeedsManualReview = false;

                    return lastAttempt;
                }

                lastFailureReason = string.Join("; ", lastValidation.Errors);

                Console.WriteLine($"Extraction attempt {attempt}/{_maxAttempts} failed validation: {lastFailureReason}");
            }

            // Exhausted retries — hand back the best available result rather than failing the
            // whole upload, but make the uncertainty explicit so it isn't trusted silently. If
            // every attempt threw (no successful structuring at all), fall back to an empty
            // record so the caller still gets a well-formed DTO instead of an exception.
            var result = lastAttempt ?? new ExtractedAcademicRecordDto();

            result.NeedsManualReview = true;
            result.ExtractionWarnings = lastValidation.IsValid
                ? new List<string> { lastFailureReason }
                : lastValidation.Errors.Concat(lastValidation.Warnings).ToList();

            return result;
        }
    }
}
