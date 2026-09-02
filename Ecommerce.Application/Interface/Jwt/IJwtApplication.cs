using Ecommerce.Application.Dto.Jwt;
using Ecommerce.Domain.Entities.Jwt;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Application.Interface.Jwt
{
    public interface IJwtApplication
    {
        string GenerateToken(User entity);
    }
}
