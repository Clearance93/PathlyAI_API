using Microsoft.EntityFrameworkCore;
using Pathly_Data;
using Pathly_Models;
using PathlyInterfaces;

namespace PathlyRepository
{
    public class PlanRepository : GenericRepository<Plan>, IPlanRepositoryInterface
    {
        private readonly ApplicationDbContext _Context;

        public PlanRepository(ApplicationDbContext context) : base(context)
        {
            _Context = context;
        }

        public async Task<Plan?> GetByCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return null;
            }

            return await _Context.Plans.FirstOrDefaultAsync(p => p.Code == code);
        }

        public async Task<IEnumerable<Plan>> GetActivePlansAsync()
        {
            return await _Context.Plans
                .Where(p => p.IsActive)
                .OrderBy(p => p.DisplayOrder)
                .ToListAsync();
        }
    }
}
