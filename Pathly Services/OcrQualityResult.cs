namespace Pathly_Services
{
    /// <summary>What the OCR stage produced, and whether it is trustworthy enough to structure.</summary>
    public class OcrQualityResult
    {
        /// <summary>True when the OCR output looks like a usable academic transcript, not garbage.</summary>
        public bool IsUsable { get; set; }

        /// <summary>True when the text is probably a transcript but the OCR was clearly lossy (broken words, decoupled columns).</summary>
        public bool IsLowQuality { get; set; }

        /// <summary>Reason string to surface to the user when the result is not usable.</summary>
        public string? Reason { get; set; }
    }
}
