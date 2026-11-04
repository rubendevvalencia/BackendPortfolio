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
        public ITicketsRepository _ticketsRepo {get;}
        public UnitOfWork(TicketDbContext ticketDbContext, IEmployeeRepository employeeRepository, ITicketsRepository ticketsRepository)
        {
            _dbContext = ticketDbContext;
            _employeesRepo = employeeRepository;
            _ticketsRepo = ticketsRepository;
        } 

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => await _dbContext.SaveChangesAsync(cancellationToken);
    }
}


