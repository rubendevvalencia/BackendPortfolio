using Ticketing.Command.Domain.Interface.Repository.UnitOfWork;

namespace Ticketing.Command.Infrastructure.Repository.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        //private readonly DbContextEF _dbContext;
        public UnitOfWork()
        {
            
        }

        //Un único SaveChanges por caso de uso: EF Core envuelve todos los cambios pendientes
        //en una sola transacción, de modo que se confirman o se descartan en bloque.
        //No implementa IDisposable: el ciclo de vida del DbContext lo gestiona el contenedor de DI (scoped).
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            //return _dbContext.SaveChangesAsync(cancellationToken);
            return null;
        }
    }
}
