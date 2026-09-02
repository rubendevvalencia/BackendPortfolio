using Ecommerce.Domain.Interface.IRepository;
using Ecommerce.Domain.Interface.IRepository.Jwt;
using Ecommerce.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Infrastructure.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly DbContextEF _dbContext;

        public ICustomerRepository _customers { get; }
        public IUserRepository _user { get; }

        public UnitOfWork(DbContextEF dbContext, ICustomerRepository customerRepository, IUserRepository userRepository)
        {
            _dbContext = dbContext;
            _customers = customerRepository;
            _user = userRepository;
        }

        //Un único SaveChanges por caso de uso: EF Core envuelve todos los cambios pendientes
        //en una sola transacción, de modo que se confirman o se descartan en bloque.
        //No implementa IDisposable: el ciclo de vida del DbContext lo gestiona el contenedor de DI (scoped).
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => _dbContext.SaveChangesAsync(cancellationToken);
    }
}
