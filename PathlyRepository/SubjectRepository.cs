using Microsoft.EntityFrameworkCore;
using Pathly_Data;
using Pathly_Models;
using PathlyInterfaces;

namespace PathlyRepository
{
    public class SubjectRepository : GenericRepository<Subject>, ISubjectRepositoryInterface
    {
        private readonly ApplicationDbContext _Context;

        public SubjectRepository(ApplicationDbContext context) : base(context)
        {
            _Context = context;
        }

        public async Task<Subject?> FindByNormalizedNameAsync(string normalizedName)
        {
            return await _Context.Subjects.FirstOrDefaultAsync(s => s.NormalizedName == normalizedName);
        }
    }
}
