using Pathly_Data;
using Pathly_Models;
using PathlyInterfaces;

namespace PathlyRepository
{
    public class ApsAnalysisRepository : GenericRepository<ApsAnalysis>, IApsAnalysisRepositoryInterface
    {
        public ApsAnalysisRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
