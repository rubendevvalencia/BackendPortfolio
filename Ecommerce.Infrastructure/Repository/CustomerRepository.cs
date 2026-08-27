using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Ecommerce.Domain.Interface.IRepository;
using Ecommerce.Infrastructure.Data;
using Ecommerce.Domain.Entities;

namespace Ecommerce.Infrastructure.Repository
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly DbContextEF _dbContext;

        public CustomerRepository(DbContextEF dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task<Customer?> GetByIdAsync(int id)
        {
            return await _dbContext.Customers.FindAsync(id);
        }

        public async Task<IEnumerable<Customer>> GetAllAsync()
        {
            return await _dbContext.Customers.ToListAsync();
        }

        public async Task<bool> AddAsync(Customer entity)
        {
            var result = await _dbContext.Customers.AddAsync(entity);
            await _dbContext.SaveChangesAsync();
            if (result != null) return true;
            return false;
        }

        public async Task<bool> UpdateAsync(Customer customer)
        {
            var result = _dbContext.Customers.Update(customer);
            await _dbContext.SaveChangesAsync();
            if (result != null) return true;
            return false;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            //Se carga la entidad y se marca para borrado en lugar de usar ExecuteDeleteAsync():
            //ExecuteDelete ejecuta el DELETE de inmediato, fuera del change tracker, y quedaría
            //fuera de la transacción que confirma el UnitOfWork.
            var customer = await _dbContext.Customers.FindAsync(id);
            if (customer is null) return false;

            _dbContext.Customers.Remove(customer);
            await _dbContext.SaveChangesAsync();
            return true;
        }

      

    }
}
