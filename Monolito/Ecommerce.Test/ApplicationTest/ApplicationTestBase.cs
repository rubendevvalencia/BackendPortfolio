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
using NSubstitute.Core;

namespace Ecommerce.Test.ApplicationTest
{
    //Base de los tests de Application: mapper, validadores y factorias de datos.
    public abstract class ApplicationTestBase
    {
        //AutoMapper es caro de configurar, asi que se crea una sola vez.
        protected static readonly IMapper Mapper = BuildMapper();
        protected static readonly IValidator<SignUpDto> SignUpValidator = new SignUpDtoValidator();
        protected static readonly IValidator<SignInDto> SignInValidator = new SignInValidator();

        //8 caracteres es el minimo que exigen los dos validadores.
        protected const string ValidPassword = "Password123!";


        private static IMapper BuildMapper()
        {
            //El mismo perfil que registra la DI en produccion (ver ConfigureServices).
            var configuration = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>(), NullLoggerFactory.Instance);

            //Falla si algun mapa deja una propiedad sin mapear ni marcar con Ignore().
            configuration.AssertConfigurationIsValid();

            return configuration.CreateMapper();
        }

        //Validador real: ejerce las reglas de CustomerDtoValidator de verdad.
        protected static readonly IValidator<CustomerDto> CustomerValidator = new CustomerDtoValidator();

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

        //Customer que recibe el repositorio falso. Lo rellena SaveCustomer durante el Act.
        protected Customer? _customerGuardado;

        //Se usa en When(...).Do(...): saca el Customer de la llamada y lo guarda en _customerGuardado.
        protected void SaveCustomer(CallInfo llamada)
        {
            var customerRecibido = llamada.Arg<Customer>();
            _customerGuardado = customerRecibido;
        }

       
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
