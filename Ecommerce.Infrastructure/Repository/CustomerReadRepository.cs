using System.Text;
using System.Text.Json;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interface.IRepository;
using Ecommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace Ecommerce.Infrastructure.Repository
{
    public class CustomerReadRepository : ICustomerReadRepository
    {
        private readonly DbContextEF _dbContext;
        private readonly IDistributedCache _distributedCache;
        public CustomerReadRepository(DbContextEF dbContext, IDistributedCache distributedCache)
        {
            _dbContext = dbContext;
            _distributedCache = distributedCache;
        }
        public async Task<Customer?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => await _dbContext.Customers.FindAsync(new object?[] { id }, cancellationToken);

        public async Task<IEnumerable<Customer>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            IEnumerable<Customer>? customers;

            var cacheKey = "getAllCustomers";
            var redisCategories = await _distributedCache.GetAsync(cacheKey);

            if (redisCategories != null) customers = JsonSerializer.Deserialize<IEnumerable<Customer>>(redisCategories); //Obtenemos de redis
            else
            {
                customers = await _dbContext.Customers.AsNoTracking().ToListAsync(cancellationToken);
                if(customers != null)
                {
                    //Configuramos el caché
                    var serializedCategories = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(customers));
                    var options = new DistributedCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromHours(2)) //Tiempo máximo registrado
                        .SetSlidingExpiration(TimeSpan.FromHours(1));  //El tiempo máximo que dura sino se consulta, si se consume no se borra en 1h

                    await _distributedCache.SetAsync(cacheKey, serializedCategories, options, cancellationToken);
                }
            }
            return customers;
        }
    }
}


