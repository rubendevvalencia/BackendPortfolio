using Ecommerce.Application.Dto;
using Ecommerce.Application.Feature.Customers.Commands.UpdateCustomer;
using UpdateCustomerCommandV4 = Ecommerce.Application.Feature.Customers.v4.Commands.UpdateCustomer.UpdateCustomerCommand; //Mismo nombre que el de v3: el alias evita la ambiguedad.
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

        public static void MapInto(Customer destination, UpdateCustomerCommand source)
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

        public static void MapInto(Customer destination, UpdateCustomerCommandV4 source)
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
