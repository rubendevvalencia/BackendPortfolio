using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Domain.Entities.Audit
{
    public abstract class BaseAuditEntity
    {
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
        public DateTime LastUpdatedAt { get; set;}
        public string? LastUpdatedBy { get; set; }

    }
}
