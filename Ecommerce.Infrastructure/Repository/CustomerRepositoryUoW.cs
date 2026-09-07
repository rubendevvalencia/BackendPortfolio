// ============================================================================
//  CustomerRepositoryUoW - versión CON Unit of Work (referencia comparativa)
// ----------------------------------------------------------------------------
//  Convive con CustomerRepository (versión SIN UoW) solo para poder compararlas
//  en paralelo. Por eso NO implementa ICustomerRepository: sus firmas todavía no
//  coinciden con IBaseRepository<T>.
//
//  Para adoptarla de verdad:
//    1. Cambiar IBaseRepository<T> a las firmas de abajo.
//    2. Añadir Task<int> SaveChangesAsync(CancellationToken) a IUnitOfWork.
//    3. Renombrar esta clase a CustomerRepository y añadirle ": ICustomerRepository".
//    4. Borrar la versión antigua y este comentario.
//
//  Firmas que requiere IBaseRepository<T>:
//      Task AddAsync(T entity, CancellationToken cancellationToken = default);
//      void Update(T entity);
//      void Delete(T entity);
//      Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
//      Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
// ============================================================================

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Data;
using Ecommerce.Domain.Interface.IRepository;

namespace Ecommerce.Infrastructure.Repository
{
    public class CustomerRepositoryUoW : ICustomerRepositoryUoW
    {
        private readonly DbContextEF _dbContext;

        // El DbContext está registrado como Scoped: esta MISMA instancia la comparten
        // UnitOfWork y el resto de repositorios durante todo el request. Ese contexto
        // compartido es lo que permite confirmar todos los cambios de una vez.
        public CustomerRepositoryUoW(DbContextEF dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task<Customer?> GetByIdAsync(int id) 
        {
            var result = await _dbContext.Customers.FindAsync(id);
            return result;
        }

        public Task<IEnumerable<Customer>> GetAllAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<bool> Add(Customer entity)
        {
            await _dbContext.Customers.AddAsync(entity);
            return true;
        }

        public async Task<bool> Update(Customer entity)
        {
            if (_dbContext.Entry(entity).State == EntityState.Detached) _dbContext.Customers.Update(entity);
            return true;
        }

        public async Task<bool> Delete(int id)
        {
            var entity = await _dbContext.Customers.FindAsync(id);
            if (entity == null) return false; //No existe: no hay nada que marcar para borrado.

            _dbContext.Customers.Remove(entity); //Remove es sincrono: solo marca la entidad como Deleted en el ChangeTracker.
            return true;
        }
    }
}
