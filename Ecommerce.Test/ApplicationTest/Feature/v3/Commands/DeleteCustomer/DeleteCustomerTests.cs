using Ecommerce.Application.Feature.Customers.Commands.DeleteCustomer;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interface.IRepository;
using Ecommerce.Transversal.Common.Enums;
using FluentValidation;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Ecommerce.Test.ApplicationTest.Feature.v3.Commands.DeleteCustomer
{
    //Tests del handler v3 con UnitOfWork y repositorio falsos.
    //Sin caso de validacion: DeleteCustomerValidator esta vacio en v3.
    public class DeleteCustomerTests : ApplicationTestBase
    {
        private readonly ICustomerRepositoryUoW _repository = Substitute.For<ICustomerRepositoryUoW>();
        private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
        private readonly IValidator<DeleteCustomerCommand> _validator = new DeleteCustomerValidator();
        private readonly DeleteCustomerCommandHandle _handler;

        public DeleteCustomerTests()
        {
            //El UnitOfWork devuelve nuestro repositorio falso.
            _unitOfWork._customersUoW.Returns(_repository);
            _handler = new DeleteCustomerCommandHandle(_unitOfWork, Mapper, _validator);
        }

        [Fact]
        public async Task Handle_DeleteCustomer_Ok()
        {
            //Arrange
            var existente = NewCustomer(22);
            _repository.GetByIdAsync(22, Arg.Any<CancellationToken>()).Returns(existente);

            //El commit escribe una fila.
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

            var request = new DeleteCustomerCommand { Id = 22 };

            //Act
            var response = await _handler.Handle(request, CancellationToken.None);

            //Assert: se borra el cliente y se confirma una vez.
            Assert.True(response.IsSuccess);
            Assert.True(response.Data);
            _repository.Received(1).Delete(existente);
            await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_DeleteCustomer_NotFound()
        {
            //Arrange: el cliente no existe.
            _repository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Customer?)null);

            var request = new DeleteCustomerCommand { Id = 99 };

            //Act
            var response = await _handler.Handle(request, CancellationToken.None);

            //Assert: la respuesta es NotFound.
            Assert.False(response.IsSuccess);
            Assert.Equal(ErrorType.NotFound, response.ErrorType);
            Assert.Equal("Customer with ID 99 not found.", response.Message);

            //Assert: no se borra ni se confirma.
            _repository.DidNotReceive().Delete(Arg.Any<Customer>());
            await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_DeleteCustomer_SaveFail()
        {
            //Arrange: el cliente existe pero el commit no escribe ninguna fila.
            _repository.GetByIdAsync(22, Arg.Any<CancellationToken>()).Returns(NewCustomer(22));
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(0);

            var request = new DeleteCustomerCommand { Id = 22 };

            //Act
            var response = await _handler.Handle(request, CancellationToken.None);

            //Assert: la respuesta es un fallo inesperado.
            Assert.False(response.IsSuccess);
            Assert.False(response.Data);
            Assert.Equal(ErrorType.Unexpected, response.ErrorType);
            Assert.Equal("Customer with ID 22 could not be deleted.", response.Message);
        }

        [Fact]
        public async Task Handle_DeleteCustomer_Exception()
        {
            //Arrange: falla la base de datos.
            _repository.GetByIdAsync(22, Arg.Any<CancellationToken>()).Returns(NewCustomer(22));
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException("problem in db"));

            var request = new DeleteCustomerCommand { Id = 22 };

            //Act
            Func<Task> work = () => _handler.Handle(request, CancellationToken.None);

            //Assert: la excepcion no se captura.
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(work);
            Assert.Equal("problem in db", exception.Message);
        }
    }
}
