namespace Pathly_Helper
{
    /// <summary>
    /// Thrown when the AI career-analysis provider fails or returns an unusable response.
    /// Callers (e.g. controllers) should catch this and return a controlled failure to the
    /// client rather than letting a raw exception surface. Nothing is cached when this is
    /// thrown.
    /// </summary>
    public class CareerAnalysisUnavailableException : Exception
    {
        public CareerAnalysisUnavailableException(string message, Exception? innerException = null)
            : base(message, innerException)
        {
        }
    }
}
