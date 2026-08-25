using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Infrastructure.Interface
{
    public interface IRepository<T> where T : class 
    {
        Task<T> GetByIdAsync(int id);
        Task<int> AddAsync(T repository);
        Task<bool> UpdateAsync(T repository);
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();

    }
}
