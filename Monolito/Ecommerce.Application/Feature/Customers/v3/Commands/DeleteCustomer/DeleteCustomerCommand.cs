using Ecommerce.Transversal.Common;
using MediatR;

namespace Ecommerce.Application.Feature.Customers.Commands.DeleteCustomer
{
    public class DeleteCustomerCommand : IRequest<Response<bool>>
    {
        public int Id {get; set; }
    }
}


