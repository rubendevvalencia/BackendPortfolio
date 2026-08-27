using Ecommerce.Domain.Interface.IRepository;
using Ecommerce.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Infrastructure.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly DbContextEF _dbContext;

        public ICustomerRepository Customers { get; }

        public UnitOfWork(DbContextEF dbContext, ICustomerRepository customerRepository)
        {
            _dbContext = dbContext;
            Customers = customerRepository;
        }

        //Un único SaveChanges por caso de uso: EF Core envuelve todos los cambios pendientes
        //en una sola transacción, de modo que se confirman o se descartan en bloque.
        //No implementa IDisposable: el ciclo de vida del DbContext lo gestiona el contenedor de DI (scoped).
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => _dbContext.SaveChangesAsync(cancellationToken);
    }
}
