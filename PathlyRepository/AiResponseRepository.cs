using Pathly_Data;
using Pathly_Models;
using PathlyInterfaces;

namespace PathlyRepository
{
    public class AiResponseRepository : GenericRepository<AiResponse>, IaiResponseRepositoryInterface
    {
        public AiResponseRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
