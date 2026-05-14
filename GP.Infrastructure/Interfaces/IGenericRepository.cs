using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GP.Infrastructure.Interfaces
{
    public interface IGenericRepository<T> where T : class
    {
        Task<IReadOnlyList<T>> GetAllAsync(
            CancellationToken cancellationToken = default,
            params Expression<Func<T, object>>[] includes);

        Task<IReadOnlyList<T>> GetAllAsNoTrackingAsync(
            CancellationToken cancellationToken = default,
            params Expression<Func<T, object>>[] includes);

        Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<T?> FirstOrDefaultAsync(
            Expression<Func<T, bool>> predicate,
            CancellationToken cancellationToken = default,
            params Expression<Func<T, object>>[] includes);

        Task<T?> FirstOrDefaultAsNoTrackingAsync(
            Expression<Func<T, bool>> predicate,
            CancellationToken cancellationToken = default,
            params Expression<Func<T, object>>[] includes);

        Task CreateAsync(T entity, CancellationToken cancellationToken = default);
        Task DeleteAsync(T entity, CancellationToken cancellationToken = default);
        void Update(T entity);
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
