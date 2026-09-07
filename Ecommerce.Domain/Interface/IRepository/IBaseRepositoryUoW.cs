namespace Ecommerce.Domain.Interface.IRepository
{

    public interface IBaseRepositoryUoW<T> where T : class
    {
        //Los repositorios NO confirman: solo registran la intención de cambio en el contexto.
        //La confirmación (SaveChangesAsync) la decide el caso de uso a través de IUnitOfWork.
        Task<bool> Add(T entity);
        Task<bool> Update(T entity);
        Task<bool> Delete(int id); //Devuelve false si la entidad no existe; true si queda marcada para borrado.
        Task<T?> GetByIdAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();
    }
}

