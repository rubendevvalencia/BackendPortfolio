using Ecommerce.Application.Common.Behaviours;
using Ecommerce.Application.Common.Behaviours.Exceptions;
using Ecommerce.Application.Feature.Customers.v4.Commands.CreateCustomer;
using Ecommerce.Transversal.Common;
using FluentValidation;
using MediatR;

namespace Ecommerce.Test.ApplicationTest.Common.Behaviours
{
    //Tests del behaviour por si solo: validador REAL de v4 y un "next" hecho a mano que apunta si se le llamo.
    //Si next se ejecuta, la peticion habria llegado al handler; si no, el pipeline se corto antes.
    public class ValidationBehaviourTests
    {
        [Fact]
        public async Task Handle_PeticionValida_LlamaANext()
        {
            //Arrange
            var request = new CreateCustomerCommand
            {
                CompanyName = "Test",
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

            var validators = new List<IValidator<CreateCustomerCommand>> { new CreateCustomerValidator() };
            var behaviour = new ValidationBehaviour<CreateCustomerCommand, Response<bool>>(validators);

            var vecesNext = 0;
            var respuestaDelHandler = Response<bool>.Success(true);
            RequestHandlerDelegate<Response<bool>> next = (token) =>
            {
                vecesNext++;
                return Task.FromResult(respuestaDelHandler);
            };

            //Act
            var response = await behaviour.Handle(request, next, CancellationToken.None);

            //Assert: la peticion llega al handler y el behaviour devuelve su respuesta tal cual.
            Assert.Equal(1, vecesNext);
            Assert.Same(respuestaDelHandler, response);
        }

        [Fact]
        public async Task Handle_PeticionInvalida_LanzaExcepcionYNoLlamaANext()
        {
            //Arrange: CompanyName vacio y City demasiado largo, dos propiedades con error.
            var request = new CreateCustomerCommand
            {
                CompanyName = "",
                ContactName = "Test",
                ContactTitle = "Test",
                Address = "Test",
                City = new string('a', 51),
                Region = "Test",
                PostalCode = "Test",
                Country = "Test",
                Phone = "Test",
                Fax = "Test"
            };

            var validators = new List<IValidator<CreateCustomerCommand>> { new CreateCustomerValidator() };
            var behaviour = new ValidationBehaviour<CreateCustomerCommand, Response<bool>>(validators);

            var vecesNext = 0;
            RequestHandlerDelegate<Response<bool>> next = (token) =>
            {
                vecesNext++;
                return Task.FromResult(Response<bool>.Success(true));
            };

            //Act
            var excepcion = await Assert.ThrowsAsync<ValidationExceptionCustom>(() => behaviour.Handle(request, next, CancellationToken.None));

            //Assert: el pipeline se corta antes del handler.
            Assert.Equal(0, vecesNext);

            //Assert: un BaseError por cada regla que falla, con su propiedad y su mensaje.
            var numeroDeErrores = excepcion.Errors.Count;
            Assert.Equal(2, numeroDeErrores);

            var errorCompanyName = excepcion.Errors.Single(e => e.PropertyMessage == "CompanyName");
            Assert.Equal("Company name is required.", errorCompanyName.ErrorMessage);

            var errorCity = excepcion.Errors.Single(e => e.PropertyMessage == "City");
            Assert.Equal("City cannot exceed 50 characters.", errorCity.ErrorMessage);
        }

        [Fact]
        public async Task Handle_SinValidadores_LlamaANext()
        {
            //Arrange: una peticion con la marca pero sin ningun validador registrado.
            var request = new CreateCustomerCommand();

            var validators = new List<IValidator<CreateCustomerCommand>>();
            var behaviour = new ValidationBehaviour<CreateCustomerCommand, Response<bool>>(validators);

            var vecesNext = 0;
            RequestHandlerDelegate<Response<bool>> next = (token) =>
            {
                vecesNext++;
                return Task.FromResult(Response<bool>.Success(true));
            };

            //Act
            var response = await behaviour.Handle(request, next, CancellationToken.None);

            //Assert: sin reglas no hay nada que rechazar y la peticion sigue su camino.
            Assert.Equal(1, vecesNext);
            Assert.True(response.IsSuccess);
        }
    }
}
