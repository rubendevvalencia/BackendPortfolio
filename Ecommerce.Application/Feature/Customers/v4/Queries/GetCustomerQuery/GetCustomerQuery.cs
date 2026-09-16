using Ecommerce.Application.Common.Interface;
using Ecommerce.Application.Dto;
using Ecommerce.Transversal.Common;
using MediatR;

namespace Ecommerce.Application.Feature.Customers.v4.Queries.GetCustomerQuery
{
    public class GetCustomerQuery : IRequest<Response<CustomerDto>>, IValidatableRequest //Las queries tambien pasan por el behaviour si llevan la marca
    {
        public int Id { get; set; }
    }
}
