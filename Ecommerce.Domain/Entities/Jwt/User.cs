using Ecommerce.Domain.Entities.Audit;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Domain.Entities.Jwt
{
    public class User : BaseAuditEntity
    {
        public int? Id { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? UserName { get; set; }
        public string? PasswordHash { get; set; }
    }
}
