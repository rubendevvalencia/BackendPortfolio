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

        public async Task<bool> AddAsync(Customer entity)
        {
            await _dbContext.Customers.AddAsync(entity);
            return true;
        }

        public async Task<bool> UpdateAsync(Customer customer)
        {
            _dbContext.Customers.Update(customer);
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            //Se carga la entidad y se marca para borrado en lugar de usar ExecuteDeleteAsync():
            //ExecuteDelete ejecuta el DELETE de inmediato, fuera del change tracker, y quedaría
            //fuera de la transacción que confirma el UnitOfWork.
            var customer = await _dbContext.Customers.FindAsync(id);
            if (customer is null) return false;

            _dbContext.Customers.Remove(customer);
            return true;
        }

        public async Task<IEnumerable<Customer>> GetAllAsync()
        {
            return await _dbContext.Customers.AsNoTracking().ToListAsync();
        }

        public async Task<Customer?> GetByIdAsync(int id)
        {
            return await _dbContext.Customers.FindAsync(id);
        }
    }
}
