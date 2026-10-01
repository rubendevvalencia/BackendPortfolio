using Ecommerce.Application.Common.Interface;
using Ecommerce.Application.Dto.Jwt;
using Ecommerce.Transversal.Common;
using MediatR;

namespace Ecommerce.Application.Feature.Users.Commands.SignIn
{    
    public class SignInCommand: IRequest<Response<TokenDto>>, IValidatableRequest
    {
        public string? Email { get; set; }
        public string? Password { get; set; }
    }
        
}

