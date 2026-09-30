using Pathly_DTOs;
using Pathly_Helper;
using Pathly_Interfaces.IService;

namespace Pathly_Services
{
    /// <summary>
    /// Extracts structured academic records from uploaded transcripts without any paid third-party
    /// document AI. Replaces the previous Azure Document Intelligence (prebuilt-layout) pipeline
    /// with a fully free stack:
    ///
    ///   1. Raw text extraction � PdfPig for born-digital PDFs, Tesseract OCR for photos/scans.
    ///   2. Quality gate � the OCR output is checked for the fingerprints of a failed table read
    ///      (broken subject names, marks detached from rows) before it is trusted.
    ///   3. Intelligent structuring � Groq (already used elsewhere in Pathly, generous free tier)
    ///      reasons over the raw text to produce subjects/marks/student/institution fields. This
    ///      is what gives us "intelligence" close to Azure's layout AI, and it's actually more
    ///      tolerant of messy OCR output than the old regex/table-cell heuristics were.
    /// </summary>
    public class DocumentExtractionService : IDocumentExtractionService
    {
        private const int MinimumUsableTextLength = 40;

        private readonly IDocumentStructuringService _structuringService;

        public DocumentExtractionService(IDocumentStructuringService structuringService)
        {
            _structuringService = structuringService ?? throw new ArgumentNullException(nameof(structuringService));
        }

        public async Task<ExtractedAcademicRecordDto> ExtractAcademicRecordAsync(string base64File, string mimeType, string? fileName)
        {
            var fileBytes = Convert.FromBase64String(base64File);

            ValidateContentSignature(fileBytes, mimeType);

            var (rawText, ocrQuality) = ExtractRawText(fileBytes, mimeType, fileName);

            if (ocrQuality is not null && !ocrQuality.IsUsable)
            {
                // The OCR itself failed (blank, hopelessly blurry, or the table did not survive).
                // Fail fast with a clear, user-fixable message rather than silently sending
                // garbage to Groq and returning 3 corrupted subjects.
                throw new DocumentTextExtractionException(
                    ocrQuality.Reason ?? "The scan could not be read. Please retry with a clearer photo.");
            }

            if (string.IsNullOrWhiteSpace(rawText) || rawText.Trim().Length < MinimumUsableTextLength)
            {
                throw new DocumentTextExtractionException(
                    "Could not read enough text from this file. If it's a scanned/photographed " +
                    "PDF with no text layer, please upload it as a JPG/PNG image instead so OCR can run on it.");
            }

            var compactedText = CompactText(rawText);
            var (textToSend, wasTruncated) = EnforceMaxInputSize(compactedText);

            var record = await _structuringService.StructureAcademicRecordAsync(textToSend);

            // Deterministic clean-up before anything is scored: assign canonical subject names,
            // drop rows that are not real subjects (e.g. a stray verb like "napping" or a
            // "Total" header), and merge duplicate rows within a term. Each change is surfaced
            // as a warning so the learner can double-check the extraction.
            var sanitizerWarnings = AcademicRecordSanitizer.Sanitize(record);

            if (sanitizerWarnings.Count > 0)
            {
                record.NeedsManualReview = true;
                record.ExtractionWarnings.AddRange(sanitizerWarnings);
            }

            // The driver projection (record.Subjects) is what drives the analysis. If the model
            // returned only term blocks, derive it from the highest-priority block so the empty
            // check below is meaningful and the analysis has subjects to work with.
            if (record.Subjects.Count == 0 && record.AcademicPeriods.Count > 0)
            {
                var driver = record.AcademicPeriods
                    .OrderByDescending(p => p.IsFinal)
                    .ThenByDescending(p => p.Ordinal)
                    .First();

                record.Subjects = driver.Subjects;
                record.DriverTermOrdinal = driver.Ordinal == 0 ? null : driver.Ordinal;
                record.DriverTermLabel = driver.Label;
                record.DriverIsFinal = driver.IsFinal;
            }

            // The structuring service hands back an empty record when every validation/retry
            // attempt fails, so treat "no subjects extracted" as a controlled, user-fixable
            // failure instead of letting an empty subject list crash downstream (e.g. Average()).
            if (record.Subjects.Count == 0)
            {
                throw new DocumentTextExtractionException(
                    "We couldn't identify any subjects with marks in this document. " +
                    "If it's a scanned/photographed PDF, please upload it as a JPG/PNG image " +
                    "instead so OCR can run on it.");
            }

            record.ExtractionAcademicRecordId = Guid.NewGuid();
            record.RawExtractedText = rawText;
            record.ExtractedAt = DateTime.Now;

            // The full record (with its period history) is persisted downstream by the analysis
            // service � PersistSubjectsAsync here previously wrote ORPHANED standalone subject
            // rows with no record FK (the same subjects were then written a second time, linked,
            // by CareerAnalysisService). That duplicate write is gone; no standalone write occurs
            // at extraction time.

            // A readable-but-lossy OCR pass (e.g. some subject names truncated, marks partially
            // detached) is not a hard failure � Groq may still recover most of it � but it must
            // be flagged so the UI tells the user to double-check the extracted subjects.
            if (ocrQuality?.IsLowQuality == true)
            {
                record.NeedsManualReview = true;
                record.ExtractionWarnings.Add(
                    "This scan was low quality � some subject names or marks may not have been " +
                    "read perfectly. Please double-check the extracted results against the original " +
                    "report. Re-photographing it flat and in good light will improve accuracy.");
            }

            if (wasTruncated)
            {
                // Groq's free tier caps prompt + completion tokens together per minute, so a very
                // long document (e.g. a multi-year university transcript) can't always be sent in
                // full. Rather than silently dropping the tail of the document, flag it explicitly
                // so the person � or a future chunked-extraction pass � knows to double-check.
                record.NeedsManualReview = true;
                record.ExtractionWarnings.Add(
                    "The document was long enough that part of it had to be trimmed before " +
                    "extraction to stay within the free tier's per-request size limit � please " +
                    "double-check that every subject came through.");
            }

            return record;
        }

        /// <summary>
        /// Rejects uploads whose binary contents clearly disagree with the declared MIME type
        /// (e.g. a PDF renamed to .png, or junk bytes claiming to be a PDF). Conservative: only
        /// fires when both the declared and the detected type are recognised and differ, so it
        /// never blocks a legitimate file.
        /// </summary>
        private static void ValidateContentSignature(byte[] bytes, string? mimeType)
        {
            if (bytes.Length < 4)
            {
                throw new DocumentTextExtractionException(
                    "The uploaded file appears to be empty or unreadable. Please upload your results document again.");
            }

            var actual = DetectFileKind(bytes);
            var declared = NormalizeMime(mimeType);

            if (actual != FileKind.Unknown && declared != FileKind.Unknown && actual != declared)
            {
                throw new DocumentTextExtractionException(
                    "The file's contents don't match the file type you uploaded. Please upload the " +
                    "original PDF, JPG or PNG file without renaming it.");
            }
        }

        private enum FileKind { Unknown, Pdf, Jpeg, Png }

        private static FileKind DetectFileKind(byte[] b)
        {
            // PDF: "%PDF"
            if (b[0] == 0x25 && b[1] == 0x50 && b[2] == 0x44 && b[3] == 0x46) return FileKind.Pdf;
            // JPEG: FF D8 FF
            if (b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF) return FileKind.Jpeg;
            // PNG: 89 50 4E 47
            if (b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47) return FileKind.Png;
            return FileKind.Unknown;
        }

        private static FileKind NormalizeMime(string? mimeType)
        {
            if (string.IsNullOrWhiteSpace(mimeType))
            {
                return FileKind.Unknown;
            }

            var mime = mimeType.ToLowerInvariant();

            if (mime.Contains("pdf")) return FileKind.Pdf;
            if (mime.Contains("jpeg") || mime.Contains("jpg")) return FileKind.Jpeg;
            if (mime.Contains("png")) return FileKind.Png;

            return FileKind.Unknown;
        }

        // Roughly 4 characters per token for English text. Leaves headroom for the extraction
        // system prompt (~2,200 characters) plus a minimum completion budget � see
        // GroqService.EstimateExtractionMaxTokens, which this number is deliberately kept
        // consistent with.
        private const int MaxInputCharsForExtraction = 20000;

        private static (string Text, bool WasTruncated) EnforceMaxInputSize(string text)
        {
            if (text.Length <= MaxInputCharsForExtraction)
            {
                return (text, false);
            }

            return (text[..MaxInputCharsForExtraction], true);
        }

        /// <summary>
        /// Strips blank-line padding left over from per-page extraction (PdfTextExtractor writes
        /// a blank line between every page) before the text goes to Groq. This is pure token-count
        /// hygiene � Groq's free tier caps prompt + completion tokens per minute combined, so
        /// cutting dead whitespace directly widens how large a document can be processed without
        /// hitting that limit. Nothing semantically meaningful is removed; the full original text
        /// is still preserved on <see cref="ExtractedAcademicRecordDto.RawExtractedText"/>.
        /// </summary>
        private static string CompactText(string rawText)
        {
            var lines = rawText
                .Replace("\r\n", "\n")
                .Split('\n')
                .Select(line => line.TrimEnd());

            var compacted = new List<string>();
            var previousWasBlank = false;

            foreach (var line in lines)
            {
                var isBlank = string.IsNullOrWhiteSpace(line);

                if (isBlank && previousWasBlank)
                {
                    continue;
                }

                compacted.Add(line);
                previousWasBlank = isBlank;
            }

            return string.Join("\n", compacted).Trim();
        }

        private static (string Text, OcrQualityResult? Quality) ExtractRawText(byte[] fileBytes, string mimeType, string? fileName)
        {
            var isPdf = (mimeType?.Contains("pdf", StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (fileName?.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ?? false);

            if (isPdf)
            {
                // Born-digital PDFs have a real text layer (no OCR, no quality gate needed).
                return (PdfTextExtractor.ExtractText(fileBytes), null);
            }

            // Photo/scan ? Tesseract OCR. Evaluate the raw output before trusting it.
            var ocr = ImageOcrExtractor.ExtractText(fileBytes);

            if (ocr.Blurry)
            {
                // The pre-OCR sharpness gate rejected the image. Fail fast with clear guidance.
                return (string.Empty, new OcrQualityResult
                {
                    IsUsable = false,
                    Reason = "The photo is too blurry to read the results reliably. Please " +
                             "re-photograph the report FLAT and TOP-DOWN (not at an angle), in good, " +
                             "even lighting, with the whole table sharp and in frame, then retry."
                });
            }

            var quality = OcrQualityEvaluator.Evaluate(ocr.Text, ocr.MeanConfidence);

            return (ocr.Text, quality);
        }
    }
}
