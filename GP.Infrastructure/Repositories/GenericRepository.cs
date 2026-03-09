using System.Linq.Expressions;
using GP.Infrastructure.Data;
using GP.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GP.Infrastructure.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        protected readonly ApplicationDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public GenericRepository(ApplicationDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public async Task<IReadOnlyList<T>> GetAllAsync(
            CancellationToken cancellationToken = default,
            params Expression<Func<T, object>>[] includes)
        {
            return await ApplyIncludes(_dbSet.AsQueryable(), includes)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<T>> GetAllAsNoTrackingAsync(
            CancellationToken cancellationToken = default,
            params Expression<Func<T, object>>[] includes)
        {
            return await ApplyIncludes(_dbSet.AsNoTracking(), includes)
                .ToListAsync(cancellationToken);
        }

        public async Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _dbSet.FindAsync([id], cancellationToken);
        }

        public async Task<T?> FirstOrDefaultAsync(
            Expression<Func<T, bool>> predicate,
            CancellationToken cancellationToken = default,
            params Expression<Func<T, object>>[] includes)
        {
            return await ApplyIncludes(_dbSet.AsQueryable(), includes)
                .FirstOrDefaultAsync(predicate, cancellationToken);
        }

        public async Task<T?> FirstOrDefaultAsNoTrackingAsync(
            Expression<Func<T, bool>> predicate,
            CancellationToken cancellationToken = default,
            params Expression<Func<T, object>>[] includes)
        {
            return await ApplyIncludes(_dbSet.AsNoTracking(), includes)
                .FirstOrDefaultAsync(predicate, cancellationToken);
        }

        public async Task CreateAsync(T entity, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(entity);
            await _dbSet.AddAsync(entity, cancellationToken);
        }

        public Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(entity);
            _dbSet.Remove(entity);
            return Task.CompletedTask;
        }

        public void Update(T entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            _dbSet.Update(entity);
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }

        private static IQueryable<T> ApplyIncludes(
            IQueryable<T> query,
            params Expression<Func<T, object>>[] includes)
        {
            if (includes is null || includes.Length == 0)
            {
                return query;
            }

            foreach (var include in includes)
            {
                query = query.Include(include);
            }

            return query;
        }
    }
}
