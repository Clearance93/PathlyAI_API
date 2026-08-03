using Microsoft.EntityFrameworkCore;
using Pathly_Data;
using PathlyInterfaces;
using System.Linq.Expressions;

namespace PathlyRepository
{
    public class GenericRepository<T> : IGenericInterface<T> where T : class
    {
        private readonly ApplicationDbContext _Context;
        private readonly DbSet<T> _DbSet;

        public GenericRepository(ApplicationDbContext context)
        {
            _Context = context ?? throw new ArgumentNullException(nameof(context));
            _DbSet = _Context.Set<T>();
        }

        public async Task AddAsync(T entity)
        {
            await _DbSet.AddAsync(entity);
        }

        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _DbSet.Where(predicate)
                               .ToListAsync();
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _DbSet.ToListAsync();
        }

        public async Task<T?> GetByIdAsync(Guid id)
        {
            return await _DbSet.FindAsync(id);
        }

        public async Task SaveChangesAsync()
        {
            await _Context.SaveChangesAsync();
        }

        public void Update(T entity)
        {
            _Context.Update(entity);
        }
    }
}
