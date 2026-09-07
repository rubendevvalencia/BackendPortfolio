using Microsoft.EntityFrameworkCore;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interface.IRepository;
using Ecommerce.Infrastructure.Data;

namespace Ecommerce.Infrastructure.Repository
{
    //Version del repositorio de Customer bajo el patron Unit of Work (v2 de la API).
    //No confirma en ningun metodo: el unico SaveChangesAsync de la Infraestructura vive en UnitOfWork.
    public class CustomerRepositoryUoW : ICustomerRepositoryUoW
    {
        private readonly DbContextEF _dbContext;

        //El DbContext esta registrado como Scoped: esta MISMA instancia la comparten UnitOfWork
        //y el resto de repositorios durante todo el request. Ese contexto compartido es lo que
        //permite confirmar de una sola vez los cambios de varios repositorios.
        public CustomerRepositoryUoW(DbContextEF dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Customer?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => await _dbContext.Customers.FindAsync(new object?[] { id }, cancellationToken);

        public async Task<IEnumerable<Customer>> GetAllAsync(CancellationToken cancellationToken = default)
            => await _dbContext.Customers.AsNoTracking().ToListAsync(cancellationToken);

        public async Task AddAsync(Customer entity, CancellationToken cancellationToken = default)
            => await _dbContext.Customers.AddAsync(entity, cancellationToken);

        public void Update(Customer entity)
        {
            if (_dbContext.Entry(entity).State == EntityState.Detached) _dbContext.Customers.Update(entity);
        }

        public void Delete(Customer entity) => _dbContext.Customers.Remove(entity);
    }
}
