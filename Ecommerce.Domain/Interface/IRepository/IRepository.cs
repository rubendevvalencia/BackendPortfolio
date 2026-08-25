using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Domain.Interface.IRepository
{
    public interface IRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(int id);
        Task<bool> AddAsync(T repository);
        Task<bool> UpdateAsync(T repository);
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();

    }
}
