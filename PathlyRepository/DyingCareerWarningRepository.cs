using Pathly_Data;
using Pathly_Models;
using PathlyInterfaces;

namespace PathlyRepository
{
    public class DyingCareerWarningRepository : GenericRepository<DyingCareerWarning>, IDyingCareerWarningRepositoyInterface
    {
        public DyingCareerWarningRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
