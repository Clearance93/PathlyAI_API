namespace Pathly_Helper
{
    /// <summary>
    /// Thrown when both Groq and the Azure Model Router fail or return an unusable response
    /// (Part 4, Step 5). Callers (e.g. controllers) should catch this and return a controlled
    /// failure to the client rather than letting a raw exception surface. Nothing is cached
    /// when this is thrown.
    /// </summary>
    public class CareerAnalysisUnavailableException : Exception
    {
        public CareerAnalysisUnavailableException(string message, Exception? innerException = null)
            : base(message, innerException)
        {
        }
    }
}
