namespace Ecommerce.Domain.Interface.IRepository
{
    //Contrato de repositorio bajo el patron Unit of Work.
    //Los repositorios NO confirman: solo registran la intencion de cambio en el ChangeTracker.
    //La confirmacion (SaveChangesAsync) la decide el caso de uso a traves de IUnitOfWork.
    public interface IBaseRepositoryUoW<T> where T : class
    {
        Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
        Task AddAsync(T entity, CancellationToken cancellationToken = default);
        void Update(T entity); //No notifica porque el UoW lo hace, por eso void. No necesita ser async: el cambio se registra en el ChangeTracker y la confirmacion la hace el UoW.
        void Delete(T entity); //No notifica porque el UoW lo hace, por eso void. Recibe la entidad, no el id: comprobar si existe es decision del caso de uso. No necesita ser async: el cambio se registra en el ChangeTracker y la confirmacion la hace el UoW.
        Task<bool> CompareInfoInDb(T entity, CancellationToken cancellationToken = default);
    }
}
