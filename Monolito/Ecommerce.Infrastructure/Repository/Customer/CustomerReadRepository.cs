using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interface.IRepository;
using Ecommerce.Infrastructure.Data;
using Ecommerce.Infrastructure.Data.Cache;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;

namespace Ecommerce.Infrastructure.Repository
{
    public class CustomerReadRepository : ICustomerReadRepository
    {
        private readonly DbContextEF _dbContext;
        private readonly IDistributedCache _distributedCache;
        private readonly IConfiguration _conf;
        public CustomerReadRepository(DbContextEF dbContext, IDistributedCache distributedCache, IConfiguration conf)
        {
            _dbContext = dbContext;
            _distributedCache = distributedCache;
            _conf = conf;
        }
        public async Task<Customer?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var customer = await _dbContext.Customers.FindAsync(new object?[] { id }, cancellationToken);
            return customer;
        }

        public async Task<IEnumerable<Customer>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            IEnumerable<Customer>? customers;

            eCacheKey cacheKey = eCacheKey.CustomerAll;
            var redisCategories = await _distributedCache.GetAsync(cacheKey.ToString(), cancellationToken);

            if (redisCategories != null) customers = JsonSerializer.Deserialize<IEnumerable<Customer>>(redisCategories); //Obtenemos de redis
            else
            {
                customers = await _dbContext.Customers.AsNoTracking().ToListAsync(cancellationToken);
                if(customers != null)
                {
                    //Configuramos el caché
                    var serializedCategories = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(customers));
                    var options = new DistributedCacheEntryOptions()
                        .SetAbsoluteExpiration(CacheConfiguration.Configuration(cacheKey, _conf)[0]) //Tiempo máximo registrado
                        .SetSlidingExpiration(CacheConfiguration.Configuration(cacheKey, _conf)[1]);  //El tiempo máximo que dura sino se consulta, si se consume no se borra en 1h

                    await _distributedCache.SetAsync(cacheKey.ToString(), serializedCategories, options, cancellationToken);
                }
            }
            return customers;
        }
    }
}

