using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interface.IRepository;
using Ecommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Repository
{
    public class CustomerReadRepository : ICustomerReadRepository
    {
        private readonly DbContextEF _dbContext;
        public CustomerReadRepository(DbContextEF dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task<Customer?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => await _dbContext.Customers.FindAsync(new object?[] { id }, cancellationToken);

        public async Task<IEnumerable<Customer>> GetAllAsync(CancellationToken cancellationToken = default)
            => await _dbContext.Customers.AsNoTracking().ToListAsync(cancellationToken);


    }
}


