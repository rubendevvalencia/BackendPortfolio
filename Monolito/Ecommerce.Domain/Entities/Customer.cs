using Ecommerce.Domain.Entities.Audit;
using Ecommerce.Domain.Interface;

namespace Ecommerce.Domain.Entities
{
    public class Customer : BaseAuditEntity
    {
        public int? Id { get; set; }
        public string? CompanyName { get; set; }
        public string? ContactName { get; set; }
        public string? ContactTitle { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Region { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        public string? Phone { get; set; }
        public string? Fax { get; set; }
        // Relaciones entre entidades
        public ICollection<Product>? Products { get; set; } //1:N
    }

}
