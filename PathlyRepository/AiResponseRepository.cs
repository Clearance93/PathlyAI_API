using Microsoft.EntityFrameworkCore;
using Pathly_Data;
using Pathly_Models;
using PathlyInterfaces;

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
    }
}
