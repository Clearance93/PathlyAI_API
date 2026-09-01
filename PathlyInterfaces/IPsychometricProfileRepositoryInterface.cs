using Pathly_Models;

namespace PathlyInterfaces
{
    public interface IPsychometricProfileRepositoryInterface : IGenericInterface<PsychometricProfile>
    {
        /// <summary>The user's most recently created psychometric profile, if any.</summary>
        Task<PsychometricProfile?> GetLatestByUserAsync(string applicationUserId);

        /// <summary>
        /// Returns the user's most recent profile whose six RIASEC scores exactly match the given
        /// values, so identical repeat assessments reuse the same stored profile row instead of
        /// inserting a duplicate.
        /// </summary>
        Task<PsychometricProfile?> FindLatestMatchingForUserAsync(
            string applicationUserId,
            int realistic,
            int investigative,
            int artistic,
            int social,
            int enterprising,
            int conventional);
    }
}
