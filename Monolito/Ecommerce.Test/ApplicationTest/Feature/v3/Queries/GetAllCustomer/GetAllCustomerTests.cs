using Ecommerce.Application.Feature.Customers.Queries.GetAllCustomerQuery;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interface.IRepository;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

//Namespace sin 'Query' para no chocar con la clase GetAllCustomerQuery.
namespace Ecommerce.Test.ApplicationTest.Feature.v3.Queries.GetAllCustomer
{
    //Tests del handler de lectura v3 con repositorio falso y mapper real.
    public class GetAllCustomerTests : ApplicationTestBase
    {
        private readonly ICustomerReadRepository _readRepository = Substitute.For<ICustomerReadRepository>();
        private readonly GetAllCustomerHandler _handler;

        public GetAllCustomerTests()
        {
            _handler = new GetAllCustomerHandler(_readRepository, Mapper);
        }

        [Fact]
        public async Task Handle_GetAllCustomer_Ok()
        {
            //Arrange
            string firstBusiness = "Contoso";
            string secondBusiness = "Fabrikam";
            var customers = new List<Customer>
            {
                NewCustomer(1, firstBusiness),
                NewCustomer(2, secondBusiness)
            };
            _readRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(customers);

            var request = new GetAllCustomerQuery();

            //Act
            var response = await _handler.Handle(request, CancellationToken.None);

            //Assert
            Assert.True(response.IsSuccess);
            var nombres = response.Data.Select(c => c.CompanyName);
            Assert.Equal(new[] { firstBusiness, secondBusiness }, nombres);
        }

        [Fact]
        public async Task Handle_GetAllCustomer_Empty()
        {
            //Arrange: no hay clientes.
            _readRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Customer>());

            var request = new GetAllCustomerQuery();

            //Act
            var response = await _handler.Handle(request, CancellationToken.None);

            //Assert
            Assert.True(response.IsSuccess);
            Assert.Empty(response.Data);
        }

        [Fact]
        public async Task Handle_GetAllCustomer_Exception()
        {
            //Arrange: falla la base de datos.
            _readRepository.GetAllAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException("problem in db"));

            var request = new GetAllCustomerQuery();

            //Act
            Func<Task> work = () => _handler.Handle(request, CancellationToken.None);

            //Assert: la excepcion no se captura.
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(work);
            Assert.Equal("problem in db", exception.Message);
        }
    }
}
