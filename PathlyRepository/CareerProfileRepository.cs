using Microsoft.EntityFrameworkCore;
using Pathly_Data;
using Pathly_Models;
using Pathly_Interfaces;

namespace PathlyRepository
{
    public class CareerProfileRepository : GenericRepository<CareerProfile>, ICareerProfileRepositoryInterface
    {
        private readonly ApplicationDbContext _Context;

        public CareerProfileRepository(ApplicationDbContext context) : base(context)
        {
            _Context = context;
        }

        public async Task<IReadOnlyList<CareerProfile>> GetAllCareersAsync()
        {
            return await _Context.CareerProfiles.AsNoTracking().ToListAsync();
        }
    }
}
