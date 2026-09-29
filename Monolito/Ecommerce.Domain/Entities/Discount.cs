using Ecommerce.Domain.Entities.Audit;
using Ecommerce.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Domain.Entities
{
    public class Discount : BaseAuditEntity
    {
        public int? Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public decimal? Percentage { get; set; } = 0;
        public DiscountStatus? Status { get; set; } = DiscountStatus.Inactive;
    }
}
