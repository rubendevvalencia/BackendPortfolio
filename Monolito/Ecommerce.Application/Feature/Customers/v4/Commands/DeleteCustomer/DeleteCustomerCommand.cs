using Ecommerce.Application.Common.Interface;
using Ecommerce.Transversal.Common;
using MediatR;

namespace Ecommerce.Application.Feature.Customers.v4.Commands.DeleteCustomer
{
    public class DeleteCustomerCommand : IRequest<Response<bool>>, IValidatableRequest //IValidatableRequest: lo valida ValidationBehaviour antes del handler
    {
        public int Id { get; set; }
    }
}
