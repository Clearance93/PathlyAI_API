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
            ExtractedAcademicRecordDto lastAttempt = new();
            ExtractionValidationResult lastValidation = new();

            for (var attempt = 1; attempt <= _maxAttempts; attempt++)
            {
                lastAttempt = await _inner.StructureAcademicRecordAsync(rawText);
                lastValidation = ExtractionValidator.Validate(rawText, lastAttempt);

                if (lastValidation.IsValid)
                {
                    lastAttempt.ExtractionWarnings = lastValidation.Warnings;
                    lastAttempt.NeedsManualReview = false;

                    return lastAttempt;
                }

                Console.WriteLine(
                    $"Extraction attempt {attempt}/{_maxAttempts} failed validation: " +
                    string.Join("; ", lastValidation.Errors));
            }

            // Exhausted retries — hand back the last attempt rather than failing the whole
            // upload, but make the uncertainty explicit so it isn't trusted silently.
            lastAttempt.NeedsManualReview = true;
            lastAttempt.ExtractionWarnings = lastValidation.Errors
                .Concat(lastValidation.Warnings)
                .ToList();

            return lastAttempt;
        }
    }
}
