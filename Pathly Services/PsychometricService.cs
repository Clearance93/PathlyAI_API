using Microsoft.AspNetCore.Identity;
using Pathly_Core.Unit;
using Pathly_DTOs;
using Pathly_Helper;
using Pathly_Models;
using PathlyInterfaces.IService;
using System.Text.Json;

namespace Pathly_Services
{
    /// <summary>
    /// Psychometric storage service (Controller → Service → Repository). Owns everything about
    /// persisting and retrieving a learner's answered psychometric questions: user validation,
    /// answer fingerprinting, profile dedupe and mapping back to DTOs. Analysis/LLM concerns
    /// stay in the career analysis services — this class never calls an LLM.
    /// </summary>
    public class PsychometricService : IPsychometricService
    {
        private readonly IUnitOfWork _Unit;
        private readonly UserManager<ApplicationUser> _UserManager;

        public PsychometricService(IUnitOfWork unit,
                                   UserManager<ApplicationUser> userManager)
        {
            _Unit = unit ?? throw new ArgumentNullException(nameof(unit));
            _UserManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        public async Task<PsychometricAssessmentDto> SubmitAssessmentAsync(PsychometricSubmissionDto submission)
        {
            ValidateSubmission(submission);

            var user = await _UserManager.FindByIdAsync(submission.UserId)
                       ?? throw new KeyNotFoundException($"No account exists for user id '{submission.UserId}'.");

            ValidateProfileScores(submission.Profile);

            var resultFingerprint = PsychometricAnswersFingerprint.ComputeHash(
                submission.RatingAnswers,
                submission.TrueFalseAnswers,
                submission.MultipleChoiceAnswers);

            // Identical answers already stored? Serve the stored assessment — no duplicate rows.
            var existing = await _Unit.PsychometricAssessment.FindByUserAndFingerprintAsync(user.Id, resultFingerprint);

            if (existing?.PsychometricProfile is not null)
            {
                return MapToDto(existing, servedFromExisting: true);
            }

            // Same learner, same exact score set? Reuse that profile row instead of duplicating.
            var profile = await _Unit.PsychometricProfile.FindLatestMatchingForUserAsync(
                user.Id,
                submission.Profile.Realistic,
                submission.Profile.Investigative,
                submission.Profile.Artistic,
                submission.Profile.Social,
                submission.Profile.Enterprising,
                submission.Profile.Conventional);

            if (profile is null)
            {
                profile = new PsychometricProfile
                {
                    PsychometricProfileId = Guid.NewGuid(),
                    ApplicationUserId = user.Id,
                    Realistic = submission.Profile.Realistic,
                    Investigative = submission.Profile.Investigative,
                    Artistic = submission.Profile.Artistic,
                    Social = submission.Profile.Social,
                    Enterprising = submission.Profile.Enterprising,
                    Conventional = submission.Profile.Conventional,
                    CreatedAt = DateTime.Now
                };

                await _Unit.PsychometricProfile.AddAsync(profile);
            }

            var totalQuestions = submission.RatingAnswers.Count + submission.TrueFalseAnswers.Count + submission.MultipleChoiceAnswers.Count;

            var assessment = new PsychometricAssessment
            {
                PsychometricAssessmentId = Guid.NewGuid(),
                ApplicationUserId = user.Id,
                PsychometricProfileId = profile.PsychometricProfileId,
                RatingAnswersJson = Serialize(submission.RatingAnswers),
                TrueFalseAnswersJson = Serialize(submission.TrueFalseAnswers),
                MultipleChoiceAnswersJson = Serialize(submission.MultipleChoiceAnswers),
                TotalQuestions = totalQuestions,
                AnsweredQuestions = totalQuestions,
                ResultFingerprint = resultFingerprint,
                CompletedAt = DateTime.Now
            };

            await _Unit.PsychometricAssessment.AddAsync(assessment);
            await _Unit.SaveChangesAsync();

            assessment.PsychometricProfile = profile;

            return MapToDto(assessment, servedFromExisting: false);
        }

        public async Task<PsychometricAssessmentDto?> GetLatestForUserAsync(string applicationUserId)
        {
            if (string.IsNullOrWhiteSpace(applicationUserId))
            {
                throw new ArgumentException("A user id is required.", nameof(applicationUserId));
            }

            var latest = await _Unit.PsychometricAssessment.GetLatestByUserAsync(applicationUserId);

            return latest is null ? null : MapToDto(latest, servedFromExisting: false);
        }

        private static void ValidateSubmission(PsychometricSubmissionDto submission)
        {
            if (submission is null)
            {
                throw new ArgumentNullException(nameof(submission));
            }

            if (string.IsNullOrWhiteSpace(submission.UserId))
            {
                throw new ArgumentException("The id of the logged-in user is required to store a psychometric assessment.");
            }
        }

        private static void ValidateProfileScores(PsychometricProfileDto profile)
        {
            if (profile is null)
            {
                throw new ArgumentException("The computed RIASEC scores are required.");
            }

            bool InRange(int value) => value is >= 0 and <= 100;

            if (!InRange(profile.Realistic) || !InRange(profile.Investigative) || !InRange(profile.Artistic) ||
                !InRange(profile.Social) || !InRange(profile.Enterprising) || !InRange(profile.Conventional))
            {
                throw new ArgumentException("All RIASEC scores must be between 0 and 100.");
            }
        }

        private static string? Serialize<TValue>(Dictionary<string, TValue>? answers)
        {
            return answers is null or { Count: 0 } ? null : JsonSerializer.Serialize(answers);
        }

        private static PsychometricAssessmentDto MapToDto(PsychometricAssessment assessment, bool servedFromExisting)
        {
            var profileEntity = assessment.PsychometricProfile;

            return new PsychometricAssessmentDto
            {
                PsychometricAssessmentId = assessment.PsychometricAssessmentId,
                ApplicationUserId = assessment.ApplicationUserId,
                Profile = new PsychometricProfileDto
                {
                    PsychometricProfileId = profileEntity!.PsychometricProfileId,
                    Realistic = profileEntity.Realistic,
                    Investigative = profileEntity.Investigative,
                    Artistic = profileEntity.Artistic,
                    Social = profileEntity.Social,
                    Enterprising = profileEntity.Enterprising,
                    Conventional = profileEntity.Conventional,
                    CreatedAt = profileEntity.CreatedAt
                },
                RatingAnswers = Deserialize<int>(assessment.RatingAnswersJson),
                TrueFalseAnswers = Deserialize<bool>(assessment.TrueFalseAnswersJson),
                MultipleChoiceAnswers = Deserialize<string>(assessment.MultipleChoiceAnswersJson),
                TotalQuestions = assessment.TotalQuestions,
                AnsweredQuestions = assessment.AnsweredQuestions,
                ServedFromExisting = servedFromExisting,
                CompletedAt = assessment.CompletedAt
            };
        }

        private static Dictionary<string, TValue> Deserialize<TValue>(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new Dictionary<string, TValue>();
            }

            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, TValue>>(json) ?? new Dictionary<string, TValue>();
            }
            catch (JsonException)
            {
                return new Dictionary<string, TValue>();
            }
        }
    }
}
