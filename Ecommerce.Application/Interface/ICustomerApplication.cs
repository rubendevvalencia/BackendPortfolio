using Ecommerce.Application.Dto;
using Ecommerce.Transversal.Common;

namespace Ecommerce.Application.Interface
{
    public interface ICustomerApplication
    {
        //Al especificar Response tenemos la capacidad de devolver un objeto que contiene información sobre el resultado de la operación, incluyendo si fue exitosa o no, y un mensaje adicional.
        //Esto es útil para manejar errores y proporcionar retroalimentación al usuario.
        Task<Response<CustomerDto?>> GetByIdAsync(int id);
        Task<Response<bool>> AddAsync(CustomerDto customer);
        Task<Response<bool>> UpdateAsync(CustomerDto customer);
        Task<Response<bool>> DeleteAsync(int id);
        Task<Response<IEnumerable<CustomerDto>>> GetAllAsync();
        
    }
}
