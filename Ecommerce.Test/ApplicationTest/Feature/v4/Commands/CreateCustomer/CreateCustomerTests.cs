using Ecommerce.Application.Feature.Customers.v4.Commands.CreateCustomer;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interface.IRepository;
using Ecommerce.Transversal.Common.Enums;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Ecommerce.Test.ApplicationTest.Feature.v4.Commands.CreateCustomer
{
    //Tests del handler v4 con UnitOfWork y repositorio falsos.
    //Sin caso de validacion: en v4 valida ValidationBehaviour antes del handler.
    public class CreateCustomerTests : ApplicationTestBase
    {
        private readonly ICustomerRepositoryUoW _repository = Substitute.For<ICustomerRepositoryUoW>();
        private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
        private readonly CreateCustomerCommandHandle _handler;

        public CreateCustomerTests()
        {
            //El UnitOfWork devuelve nuestro repositorio falso.
            _unitOfWork._customersUoW.Returns(_repository);
            _handler = new CreateCustomerCommandHandle(_unitOfWork, Mapper);
        }

        [Fact]
        public async Task Handle_CreateCustomer_Ok()
        {
            //Arrange
            var request = new CreateCustomerCommand
            {
                CompanyName = "Contoso",
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

            //El cliente todavia no existe en la base de datos.
            _repository.CompareInfoInDb(Arg.Any<Customer>(), Arg.Any<CancellationToken>()).Returns(false);

            //Guardamos el Customer que recibe AddAsync.
            _repository
                .When(repo => repo.AddAsync(Arg.Any<Customer>(), Arg.Any<CancellationToken>()))
                .Do(SaveCustomer);

            //El commit escribe una fila.
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

            //Act
            var response = await _handler.Handle(request, CancellationToken.None);

            //Assert: el Id va a null porque lo genera la base de datos.
            Assert.True(response.IsSuccess);
            Assert.True(response.Data);
            Assert.NotNull(_customerGuardado);
            Assert.Null(_customerGuardado.Id);
            Assert.Equal("Contoso", _customerGuardado.CompanyName);

            //Assert: se confirmo el cambio una sola vez.
            await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_CreateCustomer_Duplicated()
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

            //El cliente YA existe en la base de datos.
            _repository.CompareInfoInDb(Arg.Any<Customer>(), Arg.Any<CancellationToken>()).Returns(true);

            //Act
            var response = await _handler.Handle(request, CancellationToken.None);

            //Assert: la respuesta es un fallo por duplicado.
            Assert.False(response.IsSuccess);
            Assert.Equal(ErrorType.Duplicated, response.ErrorType);
            Assert.Equal("Customer is already registered", response.Message);

            //Assert: si ya existe, no se añade ni se confirma nada.
            await _repository.DidNotReceive().AddAsync(Arg.Any<Customer>(), Arg.Any<CancellationToken>());
            await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_CreateCustomer_SaveFail()
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

            //El cliente todavia no existe en la base de datos.
            _repository.CompareInfoInDb(Arg.Any<Customer>(), Arg.Any<CancellationToken>()).Returns(false);

            //El commit no escribe ninguna fila.
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(0);

            //Act
            var response = await _handler.Handle(request, CancellationToken.None);

            //Assert: la respuesta es un fallo inesperado.
            Assert.False(response.IsSuccess);
            Assert.False(response.Data);
            Assert.Equal(ErrorType.Unexpected, response.ErrorType);
            Assert.Equal("Customer could not be added.", response.Message);

            //Assert: el handler si intento añadir y confirmar, pero el commit no escribio nada.
            await _repository.Received(1).AddAsync(Arg.Any<Customer>(), Arg.Any<CancellationToken>());
            await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_CreateCustomer_Exception()
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

            //El cliente todavia no existe, pero falla la base de datos.
            _repository.CompareInfoInDb(Arg.Any<Customer>(), Arg.Any<CancellationToken>()).Returns(false);
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException("problem in db"));

            //Act
            Func<Task> work = () => _handler.Handle(request, CancellationToken.None);

            //Assert: la excepcion no se captura.
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(work);
            Assert.Equal("problem in db", exception.Message);
        }
    }
}
