using Ecommerce.Domain.Entity;
using Ecommerce.Infrastructure.Interface;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Infrastructure.Repository
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly DbContextDapper _dbContext;

        public CustomerRepository(DbContextDapper dbContext)
        {
            _dbContext = dbContext;
            _dbContext.CreateConnection();
        }

        public async Task<int> AddAsync(Customer entity)
        {
            /*var result = await _dbContext..AddAsync(repository);
            await _dbContext.SaveChangesAsync();
            return result;*/
            throw new NotImplementedException();
        }

        public async Task<bool> DeleteAsync(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<Customer>> GetAllAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<Customer> GetByIdAsync(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> UpdateAsync(Customer repository)
        {
            throw new NotImplementedException();
        }
    }
}
