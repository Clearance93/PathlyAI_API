namespace Pathly_Helper
{
    /// <summary>
    /// Thrown when the document's text could not be extracted (e.g. a scanned/photographed PDF
    /// with no text layer, or the OCR engine is unavailable). A controlled, user-fixable
    /// failure — surfaces as a 400 rather than a 500.
    /// </summary>
    public class DocumentTextExtractionException : Exception
    {
        public DocumentTextExtractionException(string message) : base(message)
        {
        }
    }
}
