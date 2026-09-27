using Microsoft.EntityFrameworkCore;
using Pathly_Data;
using Pathly_Models;
using Pathly_Interfaces;

namespace PathlyRepository
{
    public class AiResponseRepository : GenericRepository<AiResponse>, IaiResponseRepositoryInterface
    {
        private readonly ApplicationDbContext _Context;

        public AiResponseRepository(ApplicationDbContext context) : base(context)
        {
            _Context = context;
        }

        public async Task<AiResponse?> FindMostRecentBySubjectSetHashAsync(string subjectSetHash)
        {
            if (string.IsNullOrWhiteSpace(subjectSetHash))
            {
                return null;
            }

            return await _Context.AiResponse
                .Where(r => r.SubjectSetHash == subjectSetHash)
                .OrderByDescending(r => r.AddedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<List<AiResponse>> GetHistoryForUserAsync(string applicationUserId, int take = 100)
        {
            if (string.IsNullOrWhiteSpace(applicationUserId))
            {
                return new List<AiResponse>();
            }

            return await _Context.AiResponse
                .AsNoTracking()
                .Include(r => r.ApsAnalysis)
                .Where(r => r.ApplicationUserId == applicationUserId)
                .OrderByDescending(r => r.AddedAt)
                .Take(take)
                .ToListAsync();
        }

        public async Task<AiResponse?> GetByIdForUserAsync(Guid aiResponseId, string applicationUserId)
        {
            if (aiResponseId == Guid.Empty || string.IsNullOrWhiteSpace(applicationUserId))
            {
                return null;
            }

            return await _Context.AiResponse
                .AsNoTracking()
                .Include(r => r.ApsAnalysis)
                .FirstOrDefaultAsync(r => r.AiResponseId == aiResponseId && r.ApplicationUserId == applicationUserId);
        }
    }
}
