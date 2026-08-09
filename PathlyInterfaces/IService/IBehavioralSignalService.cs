namespace PathlyInterfaces.IService
{
    /// <summary>
    /// Placeholder for future behavioral career-interest signals (Part 14) — careers viewed,
    /// compared, courses viewed, questions asked, pathways selected, etc. Deliberately NOT
    /// wired into the analysis pipeline yet; this exists only so a future implementation has
    /// a stable seam to plug into without another architectural change. Do not build out
    /// speculative profiling behavior against this until a real feature needs it.
    /// </summary>
    public interface IBehavioralSignalService
    {
        Task RecordSignalAsync(Guid learnerReferenceId, string signalType, string? context = null);
    }
}
