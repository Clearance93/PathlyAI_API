using Pathly_Models;

namespace Pathly_Interfaces
{
    public interface ExtractedAcademicRecordInterfaceRepository : IGenericInterface<ExtractedAcademicRecord>
    {
        /// <summary>
        /// Loads a record with its persisted term history (AcademicPeriods with their subject
        /// children) and the legacy direct subject list, so a stored record can be re-analysed
        /// or mapped back to a DTO without losing the multi-term history.
        /// </summary>
        Task<ExtractedAcademicRecord?> GetByIdWithHistoryAsync(Guid id);

        /// <summary>The account's most recently uploaded academic record (with full term history).</summary>
        Task<ExtractedAcademicRecord?> GetLatestForUserAsync(string applicationUserId);

        /// <summary>All academic records owned by the account, newest first (for POPIA export/history).</summary>
        Task<List<ExtractedAcademicRecord>> GetAllForUserAsync(string applicationUserId);

        /// <summary>
        /// All of the account's academic records with their full term history (periods + subject
        /// children), oldest first — the basis for cross-upload progression. Loads subjects and
        /// periods eagerly so the caller never triggers lazy-load storms.
        /// </summary>
        Task<List<ExtractedAcademicRecord>> GetAllWithHistoryForUserAsync(string applicationUserId);
    }
}
