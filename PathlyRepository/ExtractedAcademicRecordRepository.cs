using Pathly_Data;
using Pathly_Models;
using PathlyInterfaces;

namespace PathlyRepository
{
    public class ExtractedAcademicRecordRepository : GenericRepository<ExtractedAcademicRecord>, ExtractedAcademicRecordInterfaceRepository
    {
        public ExtractedAcademicRecordRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
