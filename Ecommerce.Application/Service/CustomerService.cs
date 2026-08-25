using Ecommerce.Application.Interface.IUoW;
using Ecommerce.Domain.Entity;
using Ecommerce.Domain.Interface.IService;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Application.Service
{
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerUoW _customerUoW;
        public Task<bool> DeleteAsync(Customer customer)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Customer>> GetAllAsync()
        {
            throw new NotImplementedException();
        }

        public Task<Customer> GetByIdAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task<bool> InsertAsync(Customer customer)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UpdateAsync(Customer customer)
        {
            throw new NotImplementedException();
        }
    }
}
