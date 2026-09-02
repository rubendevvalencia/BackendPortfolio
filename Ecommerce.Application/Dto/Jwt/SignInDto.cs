using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Application.Dto.Jwt
{
    public class SignInDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
}
