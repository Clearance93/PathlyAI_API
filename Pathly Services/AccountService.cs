using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Pathly_Core.Unit;
using Pathly_DTOs;
using Pathly_Interfaces.IService;
using Pathly_Models;

namespace Pathly_Services
{
    /// <summary>
    /// Implements the account holder's POPIA rights: a full access export of their personal
    /// information, and erasure on request. Everything is scoped to the requesting account id.
    /// </summary>
    public class AccountService : IAccountService
    {
        private readonly IUnitOfWork _Unit;
        private readonly IMapper _Mapper;
        private readonly UserManager<ApplicationUser> _UserManager;
        private readonly ILogger<AccountService> _Logger;

        public AccountService(IUnitOfWork unit,
                              IMapper mapper,
                              UserManager<ApplicationUser> userManager,
                              ILogger<AccountService> logger)
        {
            _Unit = unit ?? throw new ArgumentNullException(nameof(unit));
            _Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _UserManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>Upper bound for a POPIA export/erasure — effectively "all", not the UI's page size.</summary>
        private const int AllRecords = 10000;

        public async Task<AccountExportDto> ExportDataAsync(string applicationUserId)
        {
            var user = await _UserManager.FindByIdAsync(applicationUserId)
                       ?? throw new KeyNotFoundException("No account exists for the logged-in user.");

            var analyses = await _Unit.AiResponse.GetHistoryForUserAsync(applicationUserId, AllRecords);
            var records = await _Unit.ExtractedAcademicRecord.GetAllForUserAsync(applicationUserId);
            var assessments = await _Unit.PsychometricAssessment.GetAllForUserAsync(applicationUserId);

            return new AccountExportDto
            {
                ExportedAtUtc = DateTime.UtcNow,
                Profile = new AccountProfileDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FullName = user.FullName,
                    CreatedAt = user.CreatedAt,
                    EmailConfirmed = user.EmailConfirmed,
                    TermsAcceptedAtUtc = user.TermsAcceptedAtUtc,
                    TermsVersion = user.TermsVersion,
                    MarketingConsent = user.MarketingConsent
                },
                Analyses = analyses.Select(r => new AnalysisHistoryItemDto
                {
                    AiResponseId = r.AiResponseId,
                    ExtractionAcademicRecordId = r.ExtractionAcademicRecordId,
                    GeneratedAt = r.AddedAt,
                    StudyLevel = r.Grade,
                    DriverTermLabel = r.DriverTermLabel,
                    OverallScore = r.OverallScore,
                    CalculatedAps = r.ApsAnalysis?.CalculatedAps,
                    IsPremium = r.IsPremium,
                    AcademicPersonality = r.AcademicPersonality
                }).ToList(),
                AcademicRecords = records.Select(r => _Mapper.Map<ExtractedAcademicRecordDto>(r)).ToList(),
                PsychometricAssessments = assessments.Select(MapPsychometric).ToList()
            };
        }

        public async Task DeleteAccountAsync(string applicationUserId)
        {
            var user = await _UserManager.FindByIdAsync(applicationUserId)
                       ?? throw new KeyNotFoundException("No account exists for the logged-in user.");

            // Erase the personal analysis/assessment data the account owns. Billing/usage
            // transactions are retained for accounting and legal-record purposes (POPIA permits
            // retention where another law or a legitimate business record requires it).
            foreach (var response in await _Unit.AiResponse.GetHistoryForUserAsync(applicationUserId, AllRecords))
            {
                _Unit.AiResponse.Remove(response);
            }

            foreach (var record in await _Unit.ExtractedAcademicRecord.GetAllForUserAsync(applicationUserId))
            {
                _Unit.ExtractedAcademicRecord.Remove(record);
            }

            foreach (var assessment in await _Unit.PsychometricAssessment.GetAllForUserAsync(applicationUserId))
            {
                _Unit.PsychometricAssessment.Remove(assessment);
            }

            foreach (var profile in await _Unit.PsychometricProfile.GetAllForUserAsync(applicationUserId))
            {
                _Unit.PsychometricProfile.Remove(profile);
            }

            await _Unit.SaveChangesAsync();

            var result = await _UserManager.DeleteAsync(user);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"We couldn't delete your account: {errors}");
            }

            _Logger.LogInformation("Account {UserId} and its personal data were deleted on request.", applicationUserId);
        }

        private static PsychometricAssessmentDto MapPsychometric(PsychometricAssessment assessment)
        {
            var profile = assessment.PsychometricProfile;

            return new PsychometricAssessmentDto
            {
                PsychometricAssessmentId = assessment.PsychometricAssessmentId,
                ApplicationUserId = assessment.ApplicationUserId,
                Profile = profile is null
                    ? new PsychometricProfileDto()
                    : new PsychometricProfileDto
                    {
                        PsychometricProfileId = profile.PsychometricProfileId,
                        Realistic = profile.Realistic,
                        Investigative = profile.Investigative,
                        Artistic = profile.Artistic,
                        Social = profile.Social,
                        Enterprising = profile.Enterprising,
                        Conventional = profile.Conventional,
                        CreatedAt = profile.CreatedAt
                    },
                TotalQuestions = assessment.TotalQuestions,
                AnsweredQuestions = assessment.AnsweredQuestions,
                CompletedAt = assessment.CompletedAt
            };
        }
    }
}
