using Ecommerce.Domain.Entities.Audit;
using Ecommerce.Domain.Enum;

namespace Ecommerce.Domain.Entities
{
    public class Product : BaseAuditEntity
    {
        public int? Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public decimal? Price { get; set; } = 0;
        public int? StockQuantity { get; set; } = 0;
        public int? CategoryId { get; set; }
        public Category? Category { get; set; }

        //Relación con la entidad Customer
        public Customer? Customer { get; set; }
        public int? CustomerId { get; set; }
    }
}


