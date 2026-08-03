namespace Pathly_DTOs
{
    public class AcademicRecordUploadDto
    {
        public string Base64File { get; set; } = string.Empty;

        public string MimeType { get; set; } = string.Empty;

        public string? FileName { get; set; }
    }
}