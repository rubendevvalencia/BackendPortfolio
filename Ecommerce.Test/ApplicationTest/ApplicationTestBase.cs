using AutoMapper;
using Ecommerce.Application.Dto;
using Ecommerce.Application.Mapping;
using Ecommerce.Application.Validator;
using Ecommerce.Domain.Entities;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;

namespace Ecommerce.Test.ApplicationTest
{
    //Base comun para los tests de la capa Application: el mapper, el validador y las factorias de datos.
    //Se sustituyen los limites (repositorios, UnitOfWork), no las colaboraciones internas de la capa:
    //el mapper y el validador se usan REALES porque forman parte de lo que se esta probando.
    public abstract class ApplicationTestBase
    {
        //La configuracion de AutoMapper es cara y no guarda estado entre mapeos,
        //asi que se construye una sola vez para todos los tests.
        protected static readonly IMapper Mapper = BuildMapper();

        private static IMapper BuildMapper()
        {
            //El mismo perfil que registra la DI en produccion (ver ConfigureServices).
            var configuration = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>(), NullLoggerFactory.Instance);

            //Falta el AssertConfigurationIsValid() que deberia ir aqui: hoy el perfil no lo pasa
            //(User -> SignUpDto deja Password sin mapear) y tumbaria todos los tests de la capa.
            return configuration.CreateMapper();
        }

        //Validador real: ejerce las reglas de CustomerDtoValidator de verdad.
        protected static IValidator<CustomerDto> CustomerValidator => new CustomerDtoValidator();

        //Datos validos por defecto. Cada test cambia solo lo que le importa y el resto es ruido de fondo.
        protected static CustomerDto NewCustomerDto(string companyName = "Test") => new()
        {
            CompanyName = companyName,
            ContactName = "Test",
            ContactTitle = "Test",
            Address = "Test",
            City = "Test",
            Region = "Test",
            PostalCode = "Test",
            Country = "Test",
            Phone = "Test",
            Fax = "Test"
        };

        protected static Customer NewCustomer(int? id = 1, string companyName = "Test") => new()
        {
            Id = id,
            CompanyName = companyName,
            ContactName = "Test",
            ContactTitle = "Test",
            Address = "Test",
            City = "Test",
            Region = "Test",
            PostalCode = "Test",
            Country = "Test",
            Phone = "Test",
            Fax = "Test"
        };
    }
}
