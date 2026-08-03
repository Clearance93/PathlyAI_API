using Pathly_Data;
using Pathly_Models;
using PathlyInterfaces;

namespace PathlyRepository
{
    public class CareerMatchRepository : GenericRepository<CareerMatch>, ICareerMatchRepositoryInterface
    {
        public CareerMatchRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
