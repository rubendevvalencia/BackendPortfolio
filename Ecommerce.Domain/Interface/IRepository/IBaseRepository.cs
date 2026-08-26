using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Domain.Interface.IRepository
{
    public interface IBaseRepository<T> where T : class
    {
        Task<bool> AddAsync(T entity);
        Task<bool> UpdateAsync(T entity);
        Task<T?> GetByIdAsync(int id);
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();
    }
}
