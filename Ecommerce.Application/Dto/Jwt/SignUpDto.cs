using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Application.Dto.Jwt
{
    public class SignUpDto
    {
        public string? FirstName { get; set; } = string.Empty;
        public string? LastName { get; set; } = string.Empty;
        public string? Email { get; set; } = string.Empty;
        public string? UserName { get; set; } = string.Empty;
        public string? PasswordHash { get; set; } = string.Empty;
    }
}
