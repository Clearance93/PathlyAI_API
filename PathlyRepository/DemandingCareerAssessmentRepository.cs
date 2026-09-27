using Pathly_Data;
using Pathly_Models;
using Pathly_Interfaces;

namespace PathlyRepository
{
    public class DemandingCareerAssessmentRepository : GenericRepository<DemandingCareerAssessment>, IDemandingCareerAssessmentRepositoryInterface
    {
        public DemandingCareerAssessmentRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
