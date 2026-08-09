namespace PathlyInterfaces.IService
{
    /// <summary>
    /// Marker interface for the fallback AI analysis provider. Implemented by
    /// AzureModelRouterService. See <see cref="IPrimaryCareerAiProvider"/> for why this
    /// exists as an abstraction rather than a concrete-class dependency.
    /// </summary>
    public interface IFallbackCareerAiProvider : IGroqService
    {
    }
}
