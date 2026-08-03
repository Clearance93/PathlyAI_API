using Pathly_Data;
using Pathly_Models;
using PathlyInterfaces;

namespace PathlyRepository
{
    public class DemandingCareerAssessmentRepository : GenericRepository<DemandingCareerAssessment>, IDemandingCareerAssessmentRepositoryInterface
    {
        public DemandingCareerAssessmentRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
