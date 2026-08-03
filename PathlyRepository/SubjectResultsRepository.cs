using Pathly_Data;
using Pathly_Models;
using PathlyInterfaces;

namespace PathlyRepository
{
    public class SubjectResultsRepository : GenericRepository<SubjectResults>, ISubjectResultsRepositoryInterface
    {
        public SubjectResultsRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
