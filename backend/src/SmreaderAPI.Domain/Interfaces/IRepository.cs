using System.Linq.Expressions;
using SmreaderAPI.Domain.Entities;

namespace SmreaderAPI.Domain.Interfaces;

public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    IQueryable<T> AsQueryable();
    Task<int> AddAsync(T entity);
    Task<int> UpdateAsync(T entity);
    Task<int> DeleteAsync(int id);
    Task<IEnumerable<T>> QueryAsync(string sql, object? param = null);
}
