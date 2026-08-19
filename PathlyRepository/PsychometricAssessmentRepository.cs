using Microsoft.EntityFrameworkCore;
using Pathly_Data;
using Pathly_Models;
using PathlyInterfaces;

namespace PathlyRepository
{
    public class PsychometricAssessmentRepository : GenericRepository<PsychometricAssessment>, IPsychometricAssessmentRepositoryInterface
    {
        private readonly ApplicationDbContext _Context;

        public PsychometricAssessmentRepository(ApplicationDbContext context) : base(context)
        {
            _Context = context;
        }

        public async Task<PsychometricAssessment?> GetLatestByUserAsync(string applicationUserId)
        {
            if (string.IsNullOrWhiteSpace(applicationUserId))
            {
                return null;
            }

            return await _Context.PsychometricAssessments
                .Include(a => a.PsychometricProfile)
                .Where(a => a.ApplicationUserId == applicationUserId)
                .OrderByDescending(a => a.CompletedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<PsychometricAssessment?> FindByUserAndFingerprintAsync(string applicationUserId, string resultFingerprint)
        {
            if (string.IsNullOrWhiteSpace(applicationUserId) || string.IsNullOrWhiteSpace(resultFingerprint))
            {
                return null;
            }

            return await _Context.PsychometricAssessments
                .Include(a => a.PsychometricProfile)
                .Where(a => a.ApplicationUserId == applicationUserId && a.ResultFingerprint == resultFingerprint)
                .OrderByDescending(a => a.CompletedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<PsychometricAssessment?> GetWithProfileAsync(Guid psychometricAssessmentId)
        {
            return await _Context.PsychometricAssessments
                .Include(a => a.PsychometricProfile)
                .FirstOrDefaultAsync(a => a.PsychometricAssessmentId == psychometricAssessmentId);
        }
    }
}
