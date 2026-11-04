using Microsoft.EntityFrameworkCore;
using Ticketing.Query.Domain.Entities;
using Ticketing.Query.Domain.Interfaces;
using Ticketing.Query.Infrastructure.Data;

namespace Ticketing.Query.Infrastructure.Repository
{
    public class TicketsRepository : ITicketsRepository
    {
        private readonly TicketDbContext _dbContext;
        public TicketsRepository(TicketDbContext dbContext) => _dbContext = dbContext;

        public async Task<bool> AddAsync(Ticket entity)
        {
            await _dbContext.Tickets.AddAsync(entity);
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var ticket = await _dbContext.Tickets.FindAsync(id);
            if (ticket == null) return false;

            _dbContext.Tickets.Remove(ticket);
            return true;
        }

        public async Task<IEnumerable<Ticket>> GetAllAsync() => await _dbContext.Tickets.ToListAsync();
        
        public async Task<Ticket?> GetByIdAsync(int id) => await _dbContext.Tickets.FindAsync(id);

        //Solo marca la entidad en el contexto; la transacción la confirma IUnitOfWork.SaveChangesAsync
        public Task<bool> UpdateAsync(Ticket entity)
        {
            _dbContext.Tickets.Update(entity);

            return Task.FromResult(true);
        }
    }
}


