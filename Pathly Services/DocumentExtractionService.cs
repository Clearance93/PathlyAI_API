using AutoMapper;
using Pathly_Core.Unit;
using Pathly_DTOs;
using Pathly_Models;
using PathlyInterfaces.IService;

namespace Pathly_Services
{
    /// <summary>
    /// Extracts structured academic records from uploaded transcripts without any paid third-party
    /// document AI. Replaces the previous Azure Document Intelligence (prebuilt-layout) pipeline
    /// with a fully free stack:
    ///
    ///   1. Raw text extraction — PdfPig for born-digital PDFs, Tesseract OCR for photos/scans.
    ///   2. Intelligent structuring — Groq (already used elsewhere in Pathly, generous free tier)
    ///      reasons over the raw text to produce subjects/marks/student/institution fields. This
    ///      is what gives us "intelligence" close to Azure's layout AI, and it's actually more
    ///      tolerant of messy OCR output than the old regex/table-cell heuristics were.
    /// </summary>
    public class DocumentExtractionService : IDocumentExtractionService
    {
        private const int MinimumUsableTextLength = 40;

        private readonly IDocumentStructuringService _structuringService;
        private readonly IUnitOfWork _unit;
        private readonly IMapper _mapper;

        public DocumentExtractionService(
            IDocumentStructuringService structuringService,
            IUnitOfWork unit,
            IMapper mapper)
        {
            _structuringService = structuringService ?? throw new ArgumentNullException(nameof(structuringService));
            _unit = unit ?? throw new ArgumentNullException(nameof(unit));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public async Task<ExtractedAcademicRecordDto> ExtractAcademicRecordAsync(string base64File, string mimeType, string? fileName)
        {
            var fileBytes = Convert.FromBase64String(base64File);

            var rawText = ExtractRawText(fileBytes, mimeType, fileName);

            if (string.IsNullOrWhiteSpace(rawText) || rawText.Trim().Length < MinimumUsableTextLength)
            {
                throw new InvalidOperationException(
                    "Could not read enough text from this file. If it's a scanned/photographed " +
                    "PDF with no text layer, please upload it as a JPG/PNG image instead so OCR can run on it.");
            }

            var record = await _structuringService.StructureAcademicRecordAsync(rawText);

            record.ExtractionAcademicRecordId = Guid.NewGuid();
            record.RawExtractedText = rawText;
            record.ExtractedAt = DateTime.Now;

            await PersistSubjectsAsync(record.Subjects);

            return record;
        }

        private static string ExtractRawText(byte[] fileBytes, string mimeType, string? fileName)
        {
            var isPdf = (mimeType?.Contains("pdf", StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (fileName?.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ?? false);

            return isPdf
                ? PdfTextExtractor.ExtractText(fileBytes)
                : ImageOcrExtractor.ExtractText(fileBytes);
        }

        private async Task PersistSubjectsAsync(List<ExtractedSubjectDto> subjects)
        {
            foreach (var subjectDto in subjects)
            {
                var subjectModel = _mapper.Map<ExtractedSubject>(subjectDto);

                await _unit.ExtractedSubject.AddAsync(subjectModel);
            }

            await _unit.SaveChangesAsync();
        }
    }
}
