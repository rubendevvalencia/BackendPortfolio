using Ecommerce.Domain.Entity;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Domain.Interface.IService
{
    public interface ICustomerService
    {
        Task<bool> InsertAsync(Customer customer);
        Task<bool> UpdateAsync(Customer customer);
        Task<bool> DeleteAsync(Customer customer);
        Task<Customer> GetByIdAsync(int id);
        Task<IEnumerable<Customer>> GetAllAsync();
    }
}
