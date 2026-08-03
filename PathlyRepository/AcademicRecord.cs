using Pathly_Data;
using PathlyInterfaces;

namespace PathlyRepository
{
    public class AcademicRecord : GenericRepository<AcademicRecord>, IGenericInterface<AcademicRecord>
    {
        public AcademicRecord(ApplicationDbContext context) : base(context)
        {
        }
    }
}
