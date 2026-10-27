namespace Ticketing.Query.Domain.Interfaces
{
    public interface IBaseRepository<T> where T : class
    {
        Task<bool> AddAsync(T entity);
        Task<bool> UpdateAsync(T entity);
        Task<bool> DeleteAsync(int id);
        Task<T?> GetByIdAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();

    }

    
}

