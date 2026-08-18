using Pathly_DTOs;

namespace PathlyInterfaces.IService
{
    /// <summary>
    /// Turns raw, unstructured text pulled from a transcript (via PdfPig for digital PDFs or
    /// Tesseract OCR for scanned images) into a structured <see cref="ExtractedAcademicRecordDto"/>.
    /// This is the "intelligence" layer that replaces Azure Document Intelligence's prebuilt-layout
    /// table/field parsing — instead of regex and table-cell heuristics, an LLM reasons over the
    /// raw text directly, which is far more tolerant of messy OCR output and inconsistent layouts.
    /// </summary>
    public interface IDocumentStructuringService
    {
        Task<ExtractedAcademicRecordDto> StructureAcademicRecordAsync(string rawText);
    }
}
