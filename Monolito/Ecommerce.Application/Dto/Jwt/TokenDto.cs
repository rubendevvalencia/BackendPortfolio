using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Application.Dto.Jwt
{
    public sealed record TokenDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public string TokenType { get; set; }
        public int ExpiresIn { get; set; }
    }
}
