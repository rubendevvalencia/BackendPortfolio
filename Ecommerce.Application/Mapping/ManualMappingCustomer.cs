using Ecommerce.Application.Dto;
using Ecommerce.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Application.Mapping
{
    public static class ManualMappingCustomer
    {
        public static Customer CustomerManualMap(CustomerDto customerDto)
        {

            return new Customer
            {
                CompanyName = customerDto.CompanyName ?? "string",
                ContactName = customerDto.ContactName ?? "string",
                ContactTitle = customerDto.ContactTitle ?? "string",
                Address = customerDto.Address ?? "string",
                City = customerDto.City ?? "string",
                Region = customerDto.Region ?? "string",
                PostalCode = customerDto.PostalCode ?? "string",
                Country = customerDto.Country ?? "string",
                Phone = customerDto.Phone ?? "string",
                Fax = customerDto.Fax ?? "string",
            };
        }
    }
}
