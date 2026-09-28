using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Domain.Interface.IRepository
{
    public interface IBaseRepository<T> where T : class
    {
        //Los repositorios NO confirman: solo registran la intención de cambio en el contexto.
        //La confirmación (SaveChangesAsync) la decide el caso de uso a través de IUnitOfWork.
        Task<bool> AddAsync(T entity);
        Task<bool> UpdateAsync(T entity);
        Task<bool> DeleteAsync(int id); //Devuelve false si la entidad no existe; true si queda marcada para borrado.
        Task<T?> GetByIdAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();
    }
}
