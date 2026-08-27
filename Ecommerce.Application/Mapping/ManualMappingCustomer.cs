using Ecommerce.Application.Dto;
using Ecommerce.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Application.Mapping
{
    public static class ManualMappingCustomer
    {
        public static void MapInto(Customer destination, CustomerDto source)
        {
            //Se copian los valores sobre la entidad ya trackeada para que el ChangeTracker
            //la marque como Modified. Crear una instancia nueva la dejaria Detached.
            destination.CompanyName = source.CompanyName ?? destination.CompanyName;
            destination.ContactName = source.ContactName ?? destination.ContactName;
            destination.ContactTitle = source.ContactTitle ?? destination.ContactTitle;
            destination.Address = source.Address ?? destination.Address;
            destination.City = source.City ?? destination.City;
            destination.Region = source.Region ?? destination.Region;
            destination.PostalCode = source.PostalCode ?? destination.PostalCode;
            destination.Country = source.Country ?? destination.Country;
            destination.Phone = source.Phone ?? destination.Phone;
            destination.Fax = source.Fax ?? destination.Fax;
        }
    }
}
