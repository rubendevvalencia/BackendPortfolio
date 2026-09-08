using AutoMapper;
using Ecommerce.Application.Dto;
using Ecommerce.Application.Dto.Jwt;
using Ecommerce.Application.Mapping;
using Ecommerce.Application.Validator;
using Ecommerce.Application.Validator.Jwt;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Entities.Jwt;
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

            //Revienta si algun mapa deja propiedades de destino sin mapear ni marcar como Ignore().
            //Es la red que caza una propiedad nueva en un DTO o en una entidad que nadie se acordo
            //de mapear: sin esto el mapeo la deja en su valor por defecto en silencio y el fallo
            //aparece mucho mas tarde, en un dato que llega vacio sin motivo aparente.
            configuration.AssertConfigurationIsValid();

            return configuration.CreateMapper();
        }

        //Validador real: ejerce las reglas de CustomerDtoValidator de verdad.
        protected static IValidator<CustomerDto> CustomerValidator => new CustomerDtoValidator();

        //Datos validos por defecto. Cada test cambia solo lo que le importa y el resto es ruido de fondo.
        protected CustomerDto NewCustomerDto(string companyName = "Test")
        {
            return new CustomerDto
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
        }

        protected Customer NewCustomer(int? id = 1, string companyName = "Test")
        {
            return new Customer
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

        protected static IValidator<SignUpDto> SignUpValidator => new SignUpDtoValidator();
        protected static IValidator<SignInDto> SignInValidator => new SignInValidator();

        //8 caracteres es el minimo que exigen los dos validadores.
        protected const string ValidPassword = "Password123!";

        protected SignUpDto NewSignUpDto(string email = "ruben@email.com", string userName = "ruben")
        {
            return new SignUpDto
            {
                FirstName = "Ruben",
                LastName = "Test",
                Email = email,
                UserName = userName,
                Password = ValidPassword
            };           
        }

        protected SignInDto NewSignInDto(string email = "ruben@email.com", string password = ValidPassword) 
        {
            return new SignInDto
            {
                Email = email,
                Password = password
            };
        }

        protected User NewUser(int id = 1, string email = "ruben@email.com", string userName = "ruben")
        {
            return new User
            {
                Id = id,
                FirstName = "Ruben",
                LastName = "Test",
                Email = email,
                UserName = userName,
                PasswordHash = "hash-ya-cifrado"  //Usuario ya persistido: el PasswordHash llega cifrado desde Infrastructure, nunca en claro.
            };
        }
    }
}
