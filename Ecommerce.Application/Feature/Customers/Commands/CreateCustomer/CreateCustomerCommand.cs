using Ecommerce.Transversal.Common;
using MediatR;

namespace Ecommerce.Application.Feature.Customers.Commands.CreateCustomerCommand
{
    public class CreateCustomerCommand : IRequest<Response<bool>> //Funciona como un dto
    {
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
