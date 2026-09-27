namespace Pathly_DTOs
{
    /// <summary>The account holder's own profile details, for a POPIA access request.</summary>
    public class AccountProfileDto
    {
        public string Id { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string? FullName { get; set; }

        public DateTime? CreatedAt { get; set; }

        public bool EmailConfirmed { get; set; }

        public DateTime? TermsAcceptedAtUtc { get; set; }

        public string? TermsVersion { get; set; }

        public bool MarketingConsent { get; set; }
    }

    /// <summary>
    /// Everything Pathly holds about one account, in a single machine-readable payload.
    /// POPIA gives a data subject the right to access the personal information held about them.
    /// </summary>
    public class AccountExportDto
    {
        public string ExportVersion { get; set; } = "1.0";

        public DateTime ExportedAtUtc { get; set; }

        public AccountProfileDto? Profile { get; set; }

        public List<AnalysisHistoryItemDto> Analyses { get; set; } = new();

        public List<ExtractedAcademicRecordDto> AcademicRecords { get; set; } = new();

        public List<PsychometricAssessmentDto> PsychometricAssessments { get; set; } = new();

        public string Note { get; set; } =
            "This export contains the personal information Pathly holds about you, in line with POPIA.";
    }
}
