using Pathly_Data;
using Pathly_Models;
using PathlyInterfaces;

namespace PathlyRepository
{
    public class PsychometricProfileRepository : GenericRepository<PsychometricProfile>, IPsychometricProfileRepositoryInterface
    {
        public PsychometricProfileRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
