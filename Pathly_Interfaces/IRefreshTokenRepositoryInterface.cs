using Pathly_Models;

namespace Pathly_Interfaces
{
    public interface IRefreshTokenRepositoryInterface : IGenericInterface<RefreshToken>
    {
        /// <summary>Finds a token by the SHA-256 hash of its raw value (the only form stored).</summary>
        Task<RefreshToken?> GetByHashAsync(string tokenHash);

        /// <summary>All currently-active (unrevoked, unexpired) tokens for an account.</summary>
        Task<List<RefreshToken>> GetActiveForUserAsync(string applicationUserId);
    }
}
