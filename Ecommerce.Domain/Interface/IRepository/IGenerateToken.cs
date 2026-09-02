using Ecommerce.Application.Dto.Jwt;
using Ecommerce.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Application.Interface.Jwt
{
    public interface IGenerateToken
    {
        string GenerateToken(SignUpDto entity);
        Task GenerateTokenAsync(User user);
    }
}
