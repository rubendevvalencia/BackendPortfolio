using Microsoft.EntityFrameworkCore;
using Ticketing.Query.Domain.Entities;
using Ticketing.Query.Domain.Interfaces;
using Ticketing.Query.Infrastructure.Data;

namespace Ticketing.Query.Infrastructure.Repository
{
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly TicketDbContext _dbContext;
        public EmployeeRepository(TicketDbContext dbContext) => _dbContext = dbContext;
        public Task<bool> AddAsync(Employee entity)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Employee>> GetAllAsync()
        {
            throw new NotImplementedException();
        }

        public Task<Employee?> GetByIdAsync(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<Employee?> GetByUsernameAsync(string userName) => await _dbContext.Employees.Where(o => o.Email == userName).FirstOrDefaultAsync();


        //Solo marca la entidad en el contexto; la transacción la confirma IUnitOfWork.SaveChangesAsync
        public Task<bool> UpdateAsync(Employee entity)
        {
            _dbContext.Employees.Update(entity);

            return Task.FromResult(true);
        }
    }


}


