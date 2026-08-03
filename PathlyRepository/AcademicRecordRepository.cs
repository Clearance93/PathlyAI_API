using Pathly_Data;
using Pathly_Models;
using PathlyInterfaces;

namespace PathlyRepository
{
    public class AcademicRecordRepository : GenericRepository<AiResponse>, IAcademicRecordRepositoryInterface
    {
        public AcademicRecordRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
