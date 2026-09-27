using Pathly_Data;
using Pathly_Models;
using Pathly_Interfaces;

namespace PathlyRepository
{
    public class ExtractedSubjectRepository : GenericRepository<ExtractedSubject>, IExtractedSubjectInterfaceRepository
    {
        public ExtractedSubjectRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
