using Microsoft.EntityFrameworkCore;
using Ticketing.Query.Domain.Interfaces;
using Ticketing.Query.Infrastructure.Data;

namespace Ticketing.Query.Infrastructure.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly TicketDbContext _dbContext;
        //Declaramos los repos aquí que vayamos a usar

        public UnitOfWork(TicketDbContext ticketDbContext) => _dbContext = ticketDbContext;

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => await _dbContext.SaveChangesAsync(cancellationToken);
        

    }
}


