using Ecommerce.Application.Dto;
using Ecommerce.Transversal.Common;

namespace Ecommerce.Application.Interface
{
    public interface ICustomerApplicationUoW
    {
        Task<Response<CustomerDto?>> GetByIdAsync(int id);
        Task<Response<bool>> AddAsync(CustomerDto customer, CancellationToken cancellationToken);
        Task<Response<bool>> UpdateAsync(int id, CustomerDto customer, CancellationToken cancellationToken);
        Task<Response<bool>> DeleteAsync(int id);
        Task<Response<IEnumerable<CustomerDto>>> GetAllAsync();
    }
}


