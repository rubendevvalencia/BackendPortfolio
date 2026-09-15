namespace Ecommerce.Domain.Interface.IRepository
{
    public interface IBaseReadQueriesRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
    }
}


