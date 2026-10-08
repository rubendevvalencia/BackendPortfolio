using Ecommerce.Application.Feature.Customers.v4.Queries.GetCustomerQuery;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interface.IRepository;
using Ecommerce.Transversal.Common.Enums;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

//Namespace sin 'Query' para no chocar con la clase GetCustomerQuery.
namespace Ecommerce.Test.ApplicationTest.Feature.v4.Queries.GetCustomer
{
    //Tests del handler de lectura v4 con repositorio falso y mapper real.
    public class GetCustomerTests : ApplicationTestBase
    {
        private readonly ICustomerReadRepository _readRepository = Substitute.For<ICustomerReadRepository>();
        private readonly GetCustomerHandler _handler;

        public GetCustomerTests()
        {
            _handler = new GetCustomerHandler(_readRepository, Mapper);
        }

        [Fact]
        public async Task Handle_GetCustomer_Ok()
        {
            //Arrange
            string business = "company";
            var customer = NewCustomer(22, business);
            _readRepository.GetByIdAsync(22, Arg.Any<CancellationToken>()).Returns(customer);

            var request = new GetCustomerQuery { Id = 22 };

            //Act
            var response = await _handler.Handle(request, CancellationToken.None);

            //Assert: CustomerDto no tiene Id, se comprueban sus campos.
            Assert.True(response.IsSuccess);
            Assert.NotNull(response.Data);
            Assert.Equal(business, response.Data.CompanyName);
            Assert.Equal(customer.City, response.Data.City);
        }

        [Fact]
        public async Task Handle_GetCustomer_NotFound()
        {
            //Arrange: el cliente no existe.
            _readRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Customer?)null);

            var request = new GetCustomerQuery 
            {
                Id = 99
            };

            //Act
            var response = await _handler.Handle(request, CancellationToken.None);

            //Assert: la respuesta es NotFound.
            Assert.False(response.IsSuccess);
            Assert.Equal(ErrorType.NotFound, response.ErrorType);
            Assert.Equal("Customer with ID 99 not found.", response.Message);
            Assert.Null(response.Data);
        }

        [Fact]
        public async Task Handle_GetCustomer_Exception()
        {
            //Arrange: falla la base de datos.
            _readRepository.GetByIdAsync(22, Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException("problem in db"));

            var request = new GetCustomerQuery()
            {
                Id = 22
            };

            //Act
            Func<Task> work = () => _handler.Handle(request, CancellationToken.None);

            //Assert: la excepcion no se captura.
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(work);
            Assert.Equal("problem in db", exception.Message);
        }
    }
}
