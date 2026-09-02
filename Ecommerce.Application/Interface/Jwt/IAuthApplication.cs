using Ecommerce.Application.Dto.Jwt;
using Ecommerce.Transversal.Common;

namespace Ecommerce.Application.Interface.Jwt
{
    public interface IAuthApplication
    {
        Task<Response<bool>> SignUpAsync(SignUpDto entity);
        Task<Response<TokenDto>> SingInAsync(SignInDto entity);

    }
}
