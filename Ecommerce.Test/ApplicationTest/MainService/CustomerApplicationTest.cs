using Ecommerce.Application.Feature.Customers;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interface.IRepository;
using Ecommerce.Transversal.Common.Enums;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Ecommerce.Test.ApplicationTest.MainService
{
    //Hereda de ApplicationTestBase: de ahi salen el mapper, el validador y las factorias de datos.
    public class CustomerApplicationTest : ApplicationTestBase
    {
        //El repositorio es el limite de la capa: se sustituye para decidir que devuelve
        //y para poder comprobar despues con que se le llamo.
        private readonly ICustomerRepository _repository = Substitute.For<ICustomerRepository>();

        private CustomerApplication CreateCustomerApp() => new(_repository, Mapper, CustomerValidator);

        //Excepcion con InnerException: el caso de uso desenvuelve la interna, que es la que trae
        //el motivo real cuando el fallo viene del proveedor de base de datos.
        private static Exception NewDbException() => new InvalidOperationException("wrapper", new Exception("fallo de base de datos"));

        //---------------------------------------------------------------- AddAsync

        [Fact]
        public async Task AddAsync_DevuelveExitoCuandoElRepositorioGuarda()
        {
            //Arrange: el repositorio guarda el Customer que reciba, para poder mirarlo despues
            Customer? customerGuardado = null;
            void GuardarCustomer(Customer customer) => customerGuardado = customer;

            _repository.AddAsync(Arg.Do<Customer>(GuardarCustomer)).Returns(true);
            var sut = CreateCustomerApp();
            var customerDto = NewCustomerDto("Contoso");

            //Act
            var response = await sut.AddAsync(customerDto, CancellationToken.None);

            //Assert: ademas del Response se comprueba que el caso de uso forzo el Id a null,
            //porque lo genera la base de datos y ese detalle solo se ve aqui.
            Assert.True(response.IsSuccess);
            Assert.True(response.Data);
            Assert.NotNull(customerGuardado);
            Assert.Null(customerGuardado.Id);
            Assert.Equal("Contoso", customerGuardado.CompanyName);
        }

        [Fact]
        public async Task AddAsync_DevuelveFalloDeValidacionYNoTocaElRepositorio()
        {
            //Arrange: DTO invalido contra las reglas reales, sin CompanyName.
            //Al repositorio se le programa exito a proposito: si la validacion no cortase,
            //el alta saldria bien y el unico sintoma seria el Customer capturado aqui.
            Customer? customerGuardado = null;
            void GuardarCustomer(Customer customer) => customerGuardado = customer;

            _repository.AddAsync(Arg.Do<Customer>(GuardarCustomer)).Returns(true);
            var sut = CreateCustomerApp();
            var customerDto = NewCustomerDto();
            customerDto.CompanyName = null;

            //Act
            var response = await sut.AddAsync(customerDto, CancellationToken.None);

            //Assert: sigue a null, asi que la validacion corto antes de llegar a persistencia
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
        public async Task AddAsync_DevuelveFalloCuandoElRepositorioLanzaExcepcion()
        {
            //Arrange
            _repository.AddAsync(Arg.Any<Customer>()).ThrowsAsync(NewDbException());
            var sut = CreateCustomerApp();
            var customerDto = NewCustomerDto();

            //Act
            var response = await sut.AddAsync(customerDto, CancellationToken.None);

            //Assert
            Assert.False(response.IsSuccess);
            Assert.Equal(ErrorType.Unexpected, response.ErrorType);
            Assert.Equal("fallo de base de datos", response.Message);
        }

        //---------------------------------------------------------------- GetByIdAsync

        [Fact]
        public async Task GetByIdAsync_DevuelveElClienteMapeadoADto()
        {
            //Arrange
            var customer = NewCustomer(id: 7, companyName: "Northwind");
            _repository.GetByIdAsync(7).Returns(customer);
            var sut = CreateCustomerApp();

            //Act
            var response = await sut.GetByIdAsync(7);

            //Assert: se comprueba el mapeo de verdad, porque el mapper es el real
            Assert.True(response.IsSuccess);
            Assert.NotNull(response.Data);
            Assert.Equal("Northwind", response.Data.CompanyName);
            Assert.Equal("Test", response.Data.City);
        }

        [Fact]
        public async Task GetByIdAsync_DevuelveNotFoundCuandoElClienteNoExiste()
        {
            //Arrange: el substitute sin configurar ya devuelve null
            var sut = CreateCustomerApp();

            //Act
            var response = await sut.GetByIdAsync(99);

            //Assert: el ErrorType es lo que traduce el controller a un 404
            Assert.False(response.IsSuccess);
            Assert.Equal(ErrorType.NotFound, response.ErrorType);
            Assert.Equal("Customer with ID 99 not found.", response.Message);
        }

        [Fact]
        public async Task GetByIdAsync_DevuelveFalloCuandoElRepositorioLanzaExcepcion()
        {
            //Arrange
            _repository.GetByIdAsync(Arg.Any<int>()).ThrowsAsync(NewDbException());
            var sut = CreateCustomerApp();

            //Act
            var response = await sut.GetByIdAsync(1);

            //Assert
            Assert.False(response.IsSuccess);
            Assert.Equal(ErrorType.Unexpected, response.ErrorType);
            Assert.Equal("fallo de base de datos", response.Message);
        }

        //---------------------------------------------------------------- GetAllAsync

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
            //Arrange: la coleccion vacia no es un error, es una respuesta valida
            _repository.GetAllAsync().Returns(new List<Customer>());
            var sut = CreateCustomerApp();

            //Act
            var response = await sut.GetAllAsync();

            //Assert
            Assert.True(response.IsSuccess);
            Assert.Empty(response.Data);
        }

        [Fact]
        public async Task GetAllAsync_DevuelveFalloCuandoElRepositorioLanzaExcepcion()
        {
            //Arrange
            _repository.GetAllAsync().ThrowsAsync(NewDbException());
            var sut = CreateCustomerApp();

            //Act
            var response = await sut.GetAllAsync();

            //Assert
            Assert.False(response.IsSuccess);
            Assert.Equal(ErrorType.Unexpected, response.ErrorType);
            Assert.Equal("fallo de base de datos", response.Message);
        }

        //---------------------------------------------------------------- UpdateAsync

        [Fact]
        public async Task UpdateAsync_VuelcaElDtoSobreElClienteExistente()
        {
            //Arrange: el cliente que devuelve el repositorio es la misma instancia que se muta,
            //asi que basta mirarlo despues del Act para ver que hizo ManualMappingCustomer
            var customerExistente = NewCustomer(id: 7, companyName: "Antiguo");
            _repository.GetByIdAsync(7).Returns(customerExistente);
            _repository.UpdateAsync(customerExistente).Returns(true);
            var sut = CreateCustomerApp();
            var customerDto = NewCustomerDto("Nuevo");

            //Act
            var response = await sut.UpdateAsync(7, customerDto, CancellationToken.None);

            //Assert: el Id se conserva, porque se actualiza la entidad existente y no una nueva
            Assert.True(response.IsSuccess);
            Assert.True(response.Data);
            Assert.Equal("Nuevo", customerExistente.CompanyName);
            Assert.Equal(7, customerExistente.Id);
        }

        [Fact]
        public async Task UpdateAsync_DevuelveFalloDeValidacionYNoBuscaElCliente()
        {
            //Arrange: DTO invalido contra las reglas reales, sin City
            var sut = CreateCustomerApp();
            var customerDto = NewCustomerDto();
            customerDto.City = null;

            //Act
            var response = await sut.UpdateAsync(7, customerDto, CancellationToken.None);

            //Assert: la validacion corta antes incluso de ir a buscar el cliente
            Assert.False(response.IsSuccess);
            Assert.Equal(ErrorType.Validation, response.ErrorType);
            Assert.Contains("City", response.Errors);
            await _repository.DidNotReceive().GetByIdAsync(Arg.Any<int>());
        }

        [Fact]
        public async Task UpdateAsync_DevuelveNotFoundCuandoElClienteNoExiste()
        {
            //Arrange: el substitute sin configurar ya devuelve null
            var sut = CreateCustomerApp();
            var customerDto = NewCustomerDto();

            //Act
            var response = await sut.UpdateAsync(99, customerDto, CancellationToken.None);

            //Assert: no se intenta actualizar nada que no exista
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
        public async Task UpdateAsync_DevuelveFalloCuandoElRepositorioLanzaExcepcion()
        {
            //Arrange
            _repository.GetByIdAsync(Arg.Any<int>()).ThrowsAsync(NewDbException());
            var sut = CreateCustomerApp();
            var customerDto = NewCustomerDto();

            //Act
            var response = await sut.UpdateAsync(7, customerDto, CancellationToken.None);

            //Assert
            Assert.False(response.IsSuccess);
            Assert.Equal(ErrorType.Unexpected, response.ErrorType);
            Assert.Equal("fallo de base de datos", response.Message);
        }

        //---------------------------------------------------------------- DeleteAsync

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
            //Arrange: el repositorio devuelve false cuando el cliente no existe,
            //y el caso de uso lo traduce a NotFound en vez de a un fallo generico
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
        public async Task DeleteAsync_DevuelveFalloCuandoElRepositorioLanzaExcepcion()
        {
            //Arrange
            _repository.DeleteAsync(Arg.Any<int>()).ThrowsAsync(NewDbException());
            var sut = CreateCustomerApp();

            //Act
            var response = await sut.DeleteAsync(7);

            //Assert
            Assert.False(response.IsSuccess);
            Assert.Equal(ErrorType.Unexpected, response.ErrorType);
            Assert.Equal("fallo de base de datos", response.Message);
        }
    }
}
