using Ecommerce.Application.Common.Interface;
using Ecommerce.Transversal.Common;
using MediatR;

namespace Ecommerce.Application.Feature.Users.Commands.Delete
{
    public class DeleteCommand : IRequest<Response<bool>>, IValidatableRequest
    {
        public string? Email { get; set; }
        public string? Password { get; set; }
    }
}


