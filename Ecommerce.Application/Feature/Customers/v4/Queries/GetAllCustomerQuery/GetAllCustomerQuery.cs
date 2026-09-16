using Ecommerce.Application.Dto;
using Ecommerce.Transversal.Common;
using MediatR;

namespace Ecommerce.Application.Feature.Customers.v4.Queries.GetAllCustomerQuery
{
    public class GetAllCustomerQuery : IRequest<Response<IEnumerable<CustomerDto>>>
    {
        //Sin datos de entrada que validar: no lleva IValidatableRequest y el behaviour no se aplica.
    }
}
