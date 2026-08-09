namespace PathlyInterfaces.IService
{
    /// <summary>
    /// Marker interface for the primary (cheapest) AI analysis provider. Implemented by
    /// GroqService. Exists so <c>ResilientCareerAiService</c> can depend on an abstraction
    /// instead of a concrete class, making the Groq-first/Router-fallback behavior testable
    /// with a fake (Part 16 — AI provider behavior tests).
    /// </summary>
    public interface IPrimaryCareerAiProvider : IGroqService
    {
    }
}
