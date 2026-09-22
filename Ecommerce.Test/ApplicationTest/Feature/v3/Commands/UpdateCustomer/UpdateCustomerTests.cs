using Ecommerce.Application.Feature.Customers.Commands.UpdateCustomer;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interface.IRepository;
using Ecommerce.Transversal.Common.Enums;
using FluentValidation;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Ecommerce.Test.ApplicationTest.Feature.v3.Commands.UpdateCustomer
{
    //Tests del handler v3 con UnitOfWork y repositorio falsos. Mapper y validador reales.
    public class UpdateCustomerTests : ApplicationTestBase
    {
        private readonly ICustomerRepositoryUoW _repository = Substitute.For<ICustomerRepositoryUoW>();
        private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
        private readonly IValidator<UpdateCustomerCommand> _validator = new UpdateCustomerValidator();
        private readonly UpdateCustomerCommandHandle _handler;

        public UpdateCustomerTests()
        {
            //El UnitOfWork devuelve nuestro repositorio falso.
            _unitOfWork._customersUoW.Returns(_repository);
            _handler = new UpdateCustomerCommandHandle(_unitOfWork, Mapper, _validator);
        }

        [Fact]
        public async Task Handle_UpdateCustomer_Ok()
        {
            //Arrange
            var existente = NewCustomer(22, "old name");
            _repository.GetByIdAsync(22, Arg.Any<CancellationToken>()).Returns(existente);

            //El commit escribe una fila.
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

            string nameChange = "New name";
            var request = new UpdateCustomerCommand
            {
                Id = 22,
                CompanyName = nameChange,
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

            //Act
            var response = await _handler.Handle(request, CancellationToken.None);

            //Assert: se modifica el cliente existente y conserva su Id.
            Assert.True(response.IsSuccess);
            Assert.True(response.Data);
            Assert.Equal(nameChange, existente.CompanyName);
            Assert.Equal(22, existente.Id);

            //Assert: se actualiza y se confirma una vez.
            _repository.Received(1).Update(existente);
            await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_UpdateCustomer_ValidationFail()
        {
            //Arrange: CompanyName vacio.
            var request = new UpdateCustomerCommand
            {
                Id = 22,
                CompanyName = "",
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

            //Act
            var response = await _handler.Handle(request, CancellationToken.None);

            //Assert: la respuesta es un fallo de validacion.
            Assert.False(response.IsSuccess);
            Assert.Equal(ErrorType.Validation, response.ErrorType);

            //Assert: el error viene agrupado bajo la propiedad que falla, con su mensaje.
            Assert.True(response.Errors.ContainsKey("CompanyName"));
            var erroresCompanyName = response.Errors["CompanyName"];
            Assert.Contains("Company name is required.", erroresCompanyName);

            //Assert: si la validacion falla, el handler no llega a tocar la base de datos.
            await _repository.DidNotReceive().GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
            _repository.DidNotReceive().Update(Arg.Any<Customer>());
            await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_UpdateCustomer_NotFound()
        {
            //Arrange: el cliente no existe.
            _repository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Customer?)null);

            var request = new UpdateCustomerCommand
            {
                Id = 99,
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

            //Act
            var response = await _handler.Handle(request, CancellationToken.None);

            //Assert: la respuesta es NotFound.
            Assert.False(response.IsSuccess);
            Assert.Equal(ErrorType.NotFound, response.ErrorType);
            Assert.Equal("Customer with ID 99 not found.", response.Message);

            //Assert: no se actualiza ni se confirma.
            _repository.DidNotReceive().Update(Arg.Any<Customer>());
            await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        //Comportamiento actual, no el deseado: pendiente nº 4 del README (0 filas -> 500).
        [Fact]
        public async Task Handle_UpdateCustomer_SaveFail()
        {
            //Arrange: el cliente existe pero el commit no escribe ninguna fila.
            _repository.GetByIdAsync(22, Arg.Any<CancellationToken>()).Returns(NewCustomer(22));
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(0);

            var request = new UpdateCustomerCommand
            {
                Id = 22,
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

            //Act
            var response = await _handler.Handle(request, CancellationToken.None);

            //Assert: la respuesta es un fallo inesperado.
            Assert.False(response.IsSuccess);
            Assert.False(response.Data);
            Assert.Equal(ErrorType.Unexpected, response.ErrorType);
            Assert.Equal("Customer with ID 22 could not be updated.", response.Message);
        }

        [Fact]
        public async Task Handle_UpdateCustomer_Exception()
        {
            //Arrange: falla la base de datos.
            _repository.GetByIdAsync(22, Arg.Any<CancellationToken>()).Returns(NewCustomer(22));
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException("problem in db"));

            var request = new UpdateCustomerCommand
            {
                Id = 22,
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

            //Act
            Func<Task> work = () => _handler.Handle(request, CancellationToken.None);

            //Assert: la excepcion no se captura.
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(work);
            Assert.Equal("problem in db", exception.Message);
        }
    }
}
