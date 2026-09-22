using Ecommerce.Application.Feature.Customers;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interface.IRepository;
using Ecommerce.Transversal.Common.Enums;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Ecommerce.Test.ApplicationTest.MainService
{
    //Tests de v1 con repositorio falso. Mapper y validador reales.
    public class CustomerApplicationTest : ApplicationTestBase
    {
        //Repositorio falso: decidimos que devuelve y comprobamos como se le llama.
        private readonly ICustomerRepository _repository = Substitute.For<ICustomerRepository>();

        private CustomerApplication CreateCustomerApp() => new(_repository, Mapper, CustomerValidator);
        private static Exception NewDbException() => new InvalidOperationException("wrapper", new Exception("problem in db"));

        [Fact]
        public async Task AddAsync_DevuelveExitoCuandoElRepositorioGuarda()
        {
            //Arrange: guardamos el Customer que recibe el repositorio.
            Customer? customerGuardado = null;
            void GuardarCustomer(Customer customer) => customerGuardado = customer;

            _repository.AddAsync(Arg.Do<Customer>(GuardarCustomer)).Returns(true);
            var sut = CreateCustomerApp();
            var customerDto = NewCustomerDto("Contoso");

            //Act
            var response = await sut.AddAsync(customerDto, CancellationToken.None);

            //Assert: el Id va a null porque lo genera la base de datos.
            Assert.True(response.IsSuccess);
            Assert.True(response.Data);
            Assert.NotNull(customerGuardado);
            Assert.Null(customerGuardado.Id);
            Assert.Equal("Contoso", customerGuardado.CompanyName);
        }

        [Fact]
        public async Task AddAsync_DevuelveFalloDeValidacionYNoTocaElRepositorio()
        {
            //Arrange: DTO sin CompanyName.
            Customer? customerGuardado = null;
            void GuardarCustomer(Customer customer) => customerGuardado = customer;

            _repository.AddAsync(Arg.Do<Customer>(GuardarCustomer)).Returns(true);
            var sut = CreateCustomerApp();
            var customerDto = NewCustomerDto();
            customerDto.CompanyName = null;

            //Act
            var response = await sut.AddAsync(customerDto, CancellationToken.None);

            //Assert: no llega al repositorio.
            Assert.False(response.IsSuccess);
            Assert.Equal(ErrorType.Validation, response.ErrorType);
            Assert.Contains("CompanyName", response.Errors);
            Assert.Null(customerGuardado);
        }

        [Fact]
        public async Task AddAsync_DevuelveFalloCuandoElRepositorioNoGuarda()
        {
            //Arrange
            _repository.AddAsync(Arg.Any<Customer>()).Returns(false);
            var sut = CreateCustomerApp();
            var customerDto = NewCustomerDto();

            //Act
            var response = await sut.AddAsync(customerDto, CancellationToken.None);

            //Assert
            Assert.False(response.IsSuccess);
            Assert.Equal(ErrorType.Unexpected, response.ErrorType);
            Assert.Equal("Customer could not be added.", response.Message);
        }

        [Fact]
        public async Task AddAsync_PropagaLaExcepcionCuandoElRepositorioFalla()
        {
            //Arrange
            _repository.AddAsync(Arg.Any<Customer>()).ThrowsAsync(NewDbException());
            var sut = CreateCustomerApp();
            var customerDto = NewCustomerDto();

            //Act
            Func<Task> work = () => sut.AddAsync(customerDto, CancellationToken.None);

            //Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(work);
            Assert.Equal("wrapper", exception.Message);
        }

        [Fact]
        public async Task GetByIdAsync_DevuelveElClienteMapeadoADto()
        {
            //Arrange
            var customer = NewCustomer(id: 7, companyName: "Northwind");
            _repository.GetByIdAsync(7).Returns(customer);
            var sut = CreateCustomerApp();

            //Act
            var response = await sut.GetByIdAsync(7);

            //Assert: se comprueba el mapeo a DTO.
            Assert.True(response.IsSuccess);
            Assert.NotNull(response.Data);
            Assert.Equal("Northwind", response.Data.CompanyName);
            Assert.Equal("Test", response.Data.City);
        }

        [Fact]
        public async Task GetByIdAsync_DevuelveNotFoundCuandoElClienteNoExiste()
        {
            //Arrange: el cliente no existe.
            var sut = CreateCustomerApp();

            //Act
            var response = await sut.GetByIdAsync(99);

            //Assert: la respuesta es NotFound.
            Assert.False(response.IsSuccess);
            Assert.Equal(ErrorType.NotFound, response.ErrorType);
            Assert.Equal("Customer with ID 99 not found.", response.Message);
        }

        [Fact]
        public async Task GetByIdAsync_PropagaLaExcepcionCuandoElRepositorioFalla()
        {
            //Arrange
            _repository.GetByIdAsync(Arg.Any<int>()).ThrowsAsync(NewDbException());
            var sut = CreateCustomerApp();

            //Act
            Func<Task> work = () => sut.GetByIdAsync(1);

            //Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(work);
            Assert.Equal("wrapper", exception.Message);
        }

        [Fact]
        public async Task GetAllAsync_DevuelveTodosLosClientesMapeadosADto()
        {
            //Arrange
            var customers = new List<Customer> { NewCustomer(1, "Uno"), NewCustomer(2, "Dos") };
            _repository.GetAllAsync().Returns(customers);
            var sut = CreateCustomerApp();

            //Act
            var response = await sut.GetAllAsync();

            //Assert
            Assert.True(response.IsSuccess);
            var resultado = response.Data.ToList();
            Assert.Equal(2, resultado.Count);
            Assert.Contains(resultado, c => c.CompanyName == "Uno");
            Assert.Contains(resultado, c => c.CompanyName == "Dos");
        }

        [Fact]
        public async Task GetAllAsync_DevuelveExitoConColeccionVaciaSiNoHayClientes()
        {
            //Arrange: no hay clientes.
            _repository.GetAllAsync().Returns(new List<Customer>());
            var sut = CreateCustomerApp();

            //Act
            var response = await sut.GetAllAsync();

            //Assert
            Assert.True(response.IsSuccess);
            Assert.Empty(response.Data);
        }

        [Fact]
        public async Task GetAllAsync_PropagaLaExcepcionCuandoElRepositorioFalla()
        {
            //Arrange
            _repository.GetAllAsync().ThrowsAsync(NewDbException());
            var sut = CreateCustomerApp();

            //Act
            Func<Task> work = () => sut.GetAllAsync();

            //Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(work);
            Assert.Equal("wrapper", exception.Message);
        }

        [Fact]
        public async Task UpdateAsync_VuelcaElDtoSobreElClienteExistente()
        {
            //Arrange
            var customerExistente = NewCustomer(id: 7, companyName: "Antiguo");
            _repository.GetByIdAsync(7).Returns(customerExistente);
            _repository.UpdateAsync(customerExistente).Returns(true);
            var sut = CreateCustomerApp();
            var customerDto = NewCustomerDto("Nuevo");

            //Act
            var response = await sut.UpdateAsync(7, customerDto, CancellationToken.None);

            //Assert: se modifica el cliente existente y conserva su Id.
            Assert.True(response.IsSuccess);
            Assert.True(response.Data);
            Assert.Equal("Nuevo", customerExistente.CompanyName);
            Assert.Equal(7, customerExistente.Id);
        }

        [Fact]
        public async Task UpdateAsync_DevuelveFalloDeValidacionYNoBuscaElCliente()
        {
            //Arrange: DTO sin City.
            var sut = CreateCustomerApp();
            var customerDto = NewCustomerDto();
            customerDto.City = null;

            //Act
            var response = await sut.UpdateAsync(7, customerDto, CancellationToken.None);

            //Assert: no se busca el cliente.
            Assert.False(response.IsSuccess);
            Assert.Equal(ErrorType.Validation, response.ErrorType);
            Assert.Contains("City", response.Errors);
            await _repository.DidNotReceive().GetByIdAsync(Arg.Any<int>());
        }

        [Fact]
        public async Task UpdateAsync_DevuelveNotFoundCuandoElClienteNoExiste()
        {
            //Arrange: el cliente no existe.
            var sut = CreateCustomerApp();
            var customerDto = NewCustomerDto();

            //Act
            var response = await sut.UpdateAsync(99, customerDto, CancellationToken.None);

            //Assert: no se actualiza nada.
            Assert.False(response.IsSuccess);
            Assert.Equal(ErrorType.NotFound, response.ErrorType);
            Assert.Equal("Customer with ID 99 not found.", response.Message);
            await _repository.DidNotReceive().UpdateAsync(Arg.Any<Customer>());
        }

        [Fact]
        public async Task UpdateAsync_DevuelveFalloCuandoElRepositorioNoActualiza()
        {
            //Arrange
            var customerExistente = NewCustomer(id: 7);
            _repository.GetByIdAsync(7).Returns(customerExistente);
            _repository.UpdateAsync(customerExistente).Returns(false);
            var sut = CreateCustomerApp();
            var customerDto = NewCustomerDto();

            //Act
            var response = await sut.UpdateAsync(7, customerDto, CancellationToken.None);

            //Assert
            Assert.False(response.IsSuccess);
            Assert.Equal(ErrorType.Unexpected, response.ErrorType);
            Assert.Equal("Customer with ID 7 could not be updated.", response.Message);
        }

        [Fact]
        public async Task UpdateAsync_PropagaLaExcepcionCuandoElRepositorioFalla()
        {
            //Arrange
            _repository.GetByIdAsync(Arg.Any<int>()).ThrowsAsync(NewDbException());
            var sut = CreateCustomerApp();
            var customerDto = NewCustomerDto();

            //Act
            Func<Task> work = () => sut.UpdateAsync(7, customerDto, CancellationToken.None);

            //Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(work);
            Assert.Equal("wrapper", exception.Message);
        }

        [Fact]
        public async Task DeleteAsync_DevuelveExitoCuandoElRepositorioBorra()
        {
            //Arrange
            _repository.DeleteAsync(7).Returns(true);
            var sut = CreateCustomerApp();

            //Act
            var response = await sut.DeleteAsync(7);

            //Assert
            Assert.True(response.IsSuccess);
            Assert.True(response.Data);
        }

        [Fact]
        public async Task DeleteAsync_DevuelveNotFoundCuandoElRepositorioNoBorra()
        {
            //Arrange: el repositorio devuelve false si el cliente no existe.
            _repository.DeleteAsync(99).Returns(false);
            var sut = CreateCustomerApp();

            //Act
            var response = await sut.DeleteAsync(99);

            //Assert
            Assert.False(response.IsSuccess);
            Assert.Equal(ErrorType.NotFound, response.ErrorType);
            Assert.Equal("Customer with ID 99 not found.", response.Message);
        }

        [Fact]
        public async Task DeleteAsync_PropagaLaExcepcionCuandoElRepositorioFalla()
        {
            //Arrange
            _repository.DeleteAsync(Arg.Any<int>()).ThrowsAsync(NewDbException());
            var sut = CreateCustomerApp();

            //Act
            Func<Task> work = () => sut.DeleteAsync(7);

            //Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(work);
            Assert.Equal("wrapper", exception.Message);
        }
    }
}
