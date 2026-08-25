using Ecommerce.Domain.Entity;
using Ecommerce.Domain.Interface.IRepository;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Ecommerce.Infrastructure.Data;

namespace Ecommerce.Infrastructure.Repository
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly DbContextEF _dbContext;

        public CustomerRepository(DbContextEF dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<int> AddAsync(Customer entity)
        {
            var result = await _dbContext.Customers.AddAsync(entity);
            await _dbContext.SaveChangesAsync();
            return (int)result.Entity.Id;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var result = await _dbContext.Customers.Where(o => o.Id == id).ExecuteDeleteAsync();
            if(result != 0) return true;
            return false;
        }

        public async Task<IEnumerable<Customer>> GetAllAsync()
        {
            return await _dbContext.Customers.ToListAsync();
        }

        public async Task<Customer?> GetByIdAsync(int id)
        {
            return await _dbContext.Customers.FindAsync(id);
        }

        public async Task<bool> UpdateAsync(Customer customer)
        {
            _dbContext.Customers.Update(customer);
            var affected = await _dbContext.SaveChangesAsync();
            return affected > 0;
        }

        /*   public async Task<IEnumerable<Customer>> GetAllAsync()
        {
            var result = await _dbContext.Customers.ToListAsync();
            if (result != null) return result;
            else throw new Exception("No customers found");
        }

        public async Task<Customer> GetByIdAsync(int id)
        {
            var result = await _dbContext.Customers.FindAsync(id);
            if(result!= null) return result;
            else throw new Exception($"Customer {id} not found");
        }

        public async Task<bool> UpdateAsync(Customer repository)
        {
            var result = _dbContext.Customers.Update(repository);
            await _dbContext.SaveChangesAsync();
            if(result != null) return true;
            return false;
        }*/
    }
}
