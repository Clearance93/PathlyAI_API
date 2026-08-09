namespace Pathly_DTOs
{
    /// <summary>
    /// Request shape for the premium (Layer 2) endpoint — the same file upload as
    /// <see cref="AcademicRecordUploadDto"/> plus the learner's psychometric profile.
    /// </summary>
    public class PremiumAcademicRecordUploadDto
    {
        public string Base64File { get; set; } = string.Empty;

        public string MimeType { get; set; } = string.Empty;

        public string? FileName { get; set; }

        public PsychometricProfileDto PsychometricProfile { get; set; } = new();
    }
}
