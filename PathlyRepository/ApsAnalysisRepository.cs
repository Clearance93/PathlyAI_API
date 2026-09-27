using Pathly_Data;
using Pathly_Models;
using Pathly_Interfaces;

namespace PathlyRepository
{
    public class ApsAnalysisRepository : GenericRepository<ApsAnalysis>, IApsAnalysisRepositoryInterface
    {
        public ApsAnalysisRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
