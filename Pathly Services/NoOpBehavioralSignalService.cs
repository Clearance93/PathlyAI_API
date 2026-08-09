using PathlyInterfaces.IService;

namespace Pathly_Services
{
    /// <summary>
    /// Default no-op implementation of <see cref="IBehavioralSignalService"/> (Part 14).
    /// Registered so the interface is resolvable, but intentionally does nothing yet —
    /// swap this out once a real behavioral-signal feature is built.
    /// </summary>
    public class NoOpBehavioralSignalService : IBehavioralSignalService
    {
        public Task RecordSignalAsync(Guid learnerReferenceId, string signalType, string? context = null)
        {
            return Task.CompletedTask;
        }
    }
}
