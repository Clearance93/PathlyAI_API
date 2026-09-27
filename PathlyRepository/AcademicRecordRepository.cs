using Pathly_Data;
using Pathly_Models;
using Pathly_Interfaces;

namespace PathlyRepository
{
    public class AcademicRecordRepository : GenericRepository<AiResponse>, IAcademicRecordRepositoryInterface
    {
        public AcademicRecordRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
