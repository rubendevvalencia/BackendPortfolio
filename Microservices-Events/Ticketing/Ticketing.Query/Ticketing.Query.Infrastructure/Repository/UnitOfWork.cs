using Microsoft.EntityFrameworkCore;
using Ticketing.Query.Domain.Interfaces;
using Ticketing.Query.Infrastructure.Data;

namespace Ticketing.Query.Infrastructure.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly TicketDbContext _dbContext;
        //Declaramos los repos aquí que vayamos a usar
        public IEmployeeRepository _employeesRepo {get;}

        public UnitOfWork(TicketDbContext ticketDbContext, IEmployeeRepository employeeRepository)
        {
            _dbContext = ticketDbContext;
            _employeesRepo = employeeRepository;
            
        } 

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => await _dbContext.SaveChangesAsync(cancellationToken);
        

    }
}


