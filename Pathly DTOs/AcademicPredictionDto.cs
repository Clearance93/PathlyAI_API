namespace Pathly_DTOs
{
    /// <summary>Per-subject direction/band projection, never a false-precision point estimate.</summary>
    public class SubjectPredictionDto
    {
        /// <summary>Canonical subject name the prediction applies to.</summary>
        public string? SubjectName { get; set; }

        /// <summary>The subject's most recent actual mark.</summary>
        public int LatestMark { get; set; }

        /// <summary>Low end of the projected band for the next same-year term, clamped 0-100.</summary>
        public int ProjectedLow { get; set; }

        /// <summary>High end of the projected band for the next same-year term, clamped 0-100.</summary>
        public int ProjectedHigh { get; set; }

        /// <summary>"up" | "down" | "steady" — direction of the underlying trend.</summary>
        public string Direction { get; set; } = "steady";

        /// <summary>True when only two data points were available and the band is deliberately wide.</summary>
        public bool LowConfidence { get; set; }
    }

    public class AcademicPredictionDto
    {
        /// <summary>True when the record qualifies for next-term prediction (≥2 terms, no final block yet, term-structured).</summary>
        public bool IsPredictionAvailable { get; set; }

        /// <summary>The term ordinal the prediction targets (last populated term + 1 within the same academic year).</summary>
        public int? NextTermOrdinal { get; set; }

        public string? NextTermLabel { get; set; }

        public List<SubjectPredictionDto> SubjectPredictions { get; set; } = new();

        /// <summary>Canonical subjects projected below 40 and/or trending down sharply — candidates for improvement advice.</summary>
        public List<string> AtRiskSubjects { get; set; } = new();

        /// <summary>Human-readable caveat explaining the confidence limits of the model.</summary>
        public string? Caveat { get; set; }
    }
}
