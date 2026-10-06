using Ecommerce.Application.Common.Interface;
using Ecommerce.Transversal.Common;
using MediatR;

namespace Ecommerce.Application.Feature.ProductToCustomer
{
    public class ProductToCustomerCommand : IRequest<Response<bool>>, IValidatableRequest
    {
        public int ProductId { get; set; }
        public int CustomerId { get; set; }

    }
}


