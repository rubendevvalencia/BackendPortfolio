using Ecommerce.Application.Common.Interface;
using Ecommerce.Transversal.Common;
using MediatR;

namespace Ecommerce.Application.Feature.Users.Commands.SignUp;

public class SignUpCommand: IRequest<Response<bool>>, IValidatableRequest 
{
    public string? FirstName { get; set; } = string.Empty;
    public string? LastName { get; set; } = string.Empty;
    public string? Email { get; set; } = string.Empty;
    public string? UserName { get; set; } = string.Empty;
    public string? Password { get; set; } = string.Empty;
}
