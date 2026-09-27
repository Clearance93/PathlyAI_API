using Pathly_DTOs;

namespace Pathly_Interfaces.IService
{
    /// <summary>POPIA data-subject rights: access (export) and erasure (delete).</summary>
    public interface IAccountService
    {
        /// <summary>Assembles all personal information held for the account into one export payload.</summary>
        Task<AccountExportDto> ExportDataAsync(string applicationUserId);

        /// <summary>Deletes the account and its personal analysis/assessment data.</summary>
        Task DeleteAccountAsync(string applicationUserId);
    }
}
