using Pathly_Data;
using Pathly_Models;
using Pathly_Interfaces;

namespace PathlyRepository
{
    public class SubjectResultsRepository : GenericRepository<SubjectResults>, ISubjectResultsRepositoryInterface
    {
        public SubjectResultsRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
