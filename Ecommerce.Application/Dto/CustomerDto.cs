using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Application.Dto
{
    public sealed class CustomerDto
    {
        //Sealed evita que a classe seja herdada, garantizando que la estructura del Dto permanezca consistente y no pueda ser modificada por herencia.
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

    }
}
