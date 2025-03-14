using System.Linq.Expressions;

namespace Serina.Semantic.Ai.Pipelines.Interfaces
{
    public interface IGenericStorage<T> where T : IStorableEntity
    {
        Task InsertAsync(T entity);
        Task InsertManyAsync(IEnumerable<T> entities);
        Task<T> GetByIdAsync(Guid id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> FilterByAsync(Expression<Func<T, bool>> filterExpression);
        Task<bool> ReplaceAsync(Guid id, T entity);
        Task<bool> DeleteByIdAsync(Guid id);
        Task<bool> DeleteManyAsync(Expression<Func<T, bool>> filterExpression);
        Task<long> CountAsync();
        Task<bool> ExistsAsync(Guid id);
    }
}
