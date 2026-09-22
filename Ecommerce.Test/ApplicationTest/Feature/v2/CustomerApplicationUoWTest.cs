using Ecommerce.Application.Dto;
using Ecommerce.Application.Feature.Customers;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interface.IRepository;
using Ecommerce.Transversal.Common.Enums;
using NSubstitute;
using NSubstitute.Core;
using NSubstitute.ExceptionExtensions;

namespace Ecommerce.Test.ApplicationTest.MainService
{
    //Tests de v2 con UnitOfWork y repositorio falsos. Mapper y validador reales.
    public class CustomerApplicationUoWTest : ApplicationTestBase
    {
        private readonly ICustomerRepositoryUoW _repository = Substitute.For<ICustomerRepositoryUoW>();
        private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
        private readonly CustomerApplicationUoW _customerAppUoW;

        //xUnit crea una instancia por test: los dobles empiezan limpios.
        public CustomerApplicationUoWTest()
        {
            //El UnitOfWork devuelve nuestro repositorio falso.
            _unitOfWork._customersUoW.Returns(_repository);
            _customerAppUoW = new CustomerApplicationUoW(_unitOfWork, Mapper, CustomerValidator);
        }

        //Simula cuantas filas escribe el commit.
        private ConfiguredCall RegistrosInsertados(int filas)
        {
           return _unitOfWork.SaveChangesAsync().ReturnsForAnyArgs(filas);
        } 

        [Fact]
        public async Task AddAsync_DevuelveExitoCuandoElCommitEscribe()
        {
            //Arrange: guardamos el Customer que recibe el repositorio.
            Customer? customerGuardado = null;
            var tarea = _repository.AddAsync(Arg.Do<Customer>(c => customerGuardado = c), Arg.Any<CancellationToken>());
            RegistrosInsertados(1);

            CustomerDto customerDto = NewCustomerDto("Contoso");

            //Act
            var response = await _customerAppUoW.AddAsync(customerDto, CancellationToken.None);

            //Assert: el Id va a null porque lo genera la base de datos.
            Assert.True(response.IsSuccess);
            Assert.True(response.Data);
            Assert.NotNull(customerGuardado);
            Assert.Null(customerGuardado.Id);
            Assert.Equal("Contoso", customerGuardado.CompanyName);
        }

        [Fact]
        public async Task AddAsync_NoTocaLaPersistenciaCuandoElDtoNoEsValido()
        {
            //Arrange: CompanyName vacio.
            CustomerDto customerDto = NewCustomerDto(string.Empty);

            //Act
            var response = await _customerAppUoW.AddAsync(customerDto, CancellationToken.None);

            //Assert: no llega al repositorio.
            Assert.False(response.IsSuccess);
            Assert.Equal(ErrorType.Validation, response.ErrorType);
            Assert.True(response.Errors.ContainsKey(nameof(CustomerDto.CompanyName)));
            await _repository.DidNotReceive().AddAsync(Arg.Any<Customer>(), Arg.Any<CancellationToken>());
            await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task AddAsync_PropagaLaExcepcionCuandoLaPersistenciaFalla()
        {
            //Arrange: falla la base de datos.
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
                       .Throws(new InvalidOperationException("problem in db"));
            CustomerDto customerDto = NewCustomerDto("Contoso");

            //Act
            Func<Task> work = () => _customerAppUoW.AddAsync(customerDto, CancellationToken.None);

            //Assert: la excepcion no se captura.
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(work);
            Assert.Equal("problem in db", exception.Message);
        }

        [Fact]
        public async Task GetByIdAsync_DevuelveElDtoCuandoElClienteExiste()
        {
            //Arrange
            Customer customer = NewCustomer(id: 7, companyName: "Contoso");
            _repository.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(customer);

            //Act
            var response = await _customerAppUoW.GetByIdAsync(7);

            //Assert: CustomerDto no tiene Id, se comprueban sus campos.
            Assert.True(response.IsSuccess);
            Assert.NotNull(response.Data);
            Assert.Equal("Contoso", response.Data.CompanyName);
            Assert.Equal(customer.City, response.Data.City);
        }

        [Fact]
        public async Task GetByIdAsync_DevuelveNotFoundCuandoElClienteNoExiste()
        {
            //Arrange: el cliente no existe.
            _repository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Customer?)null);

            //Act
            var response = await _customerAppUoW.GetByIdAsync(99);

            //Assert: la respuesta es NotFound.
            Assert.False(response.IsSuccess);
            Assert.Equal(ErrorType.NotFound, response.ErrorType);
            Assert.Null(response.Data);
        }

        [Fact]
        public async Task GetAllAsync_MapeaTodosLosClientes()
        {
            //Arrange
            var customers = new List<Customer>
            {
                NewCustomer(id: 1, companyName: "Contoso"),
                NewCustomer(id: 2, companyName: "Fabrikam")
            };
            _repository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(customers);

            //Act
            var response = await _customerAppUoW.GetAllAsync();

            //Assert
            Assert.True(response.IsSuccess);
            Assert.Equal(new[] { "Contoso", "Fabrikam" }, response.Data.Select(c => c.CompanyName));
        }

        [Fact]
        public async Task GetAllAsync_DevuelveListaVaciaCuandoNoHayClientes()
        {
            //Arrange: no hay clientes.
            _repository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Customer>());

            //Act
            var response = await _customerAppUoW.GetAllAsync();

            //Assert
            Assert.True(response.IsSuccess);
            Assert.Empty(response.Data);
        }

        [Fact]
        public async Task UpdateAsync_CopiaLosCamposSobreLaEntidadExistente()
        {
            //Arrange
            Customer existente = NewCustomer(id: 7, companyName: "Nombre viejo");
            _repository.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(existente);
            RegistrosInsertados(1);

            CustomerDto customerDto = NewCustomerDto("Nombre nuevo");

            //Act
            var response = await _customerAppUoW.UpdateAsync(7, customerDto, CancellationToken.None);

            //Assert: se modifica el cliente existente y conserva su Id.
            Assert.True(response.IsSuccess);
            Assert.True(response.Data);
            Assert.Equal("Nombre nuevo", existente.CompanyName);
            Assert.Equal(7, existente.Id);
            _repository.Received(1).Update(existente);
        }

        [Fact]
        public async Task UpdateAsync_DevuelveNotFoundCuandoElClienteNoExiste()
        {
            //Arrange
            _repository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Customer?)null);
            CustomerDto customerDto = NewCustomerDto("Contoso");

            //Act
            var response = await _customerAppUoW.UpdateAsync(99, customerDto, CancellationToken.None);

            //Assert: no se actualiza ni se confirma.
            Assert.False(response.IsSuccess);
            Assert.Equal(ErrorType.NotFound, response.ErrorType);
            _repository.DidNotReceive().Update(Arg.Any<Customer>());
            await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdateAsync_NoBuscaElClienteCuandoElDtoNoEsValido()
        {
            //Arrange: CompanyName vacio.
            CustomerDto customerDto = NewCustomerDto(string.Empty);

            //Act
            var response = await _customerAppUoW.UpdateAsync(7, customerDto, CancellationToken.None);

            //Assert
            Assert.False(response.IsSuccess);
            Assert.Equal(ErrorType.Validation, response.ErrorType);
            await _repository.DidNotReceive().GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
        }

        //Comportamiento actual, no el deseado: pendiente nº 4 del README (0 filas -> 500).
        [Fact]
        public async Task UpdateAsync_HoyDevuelveFalloConCeroFilas_PendienteDeCorregir()
        {
            //Arrange: el cliente existe pero el commit no escribe ninguna fila.
            _repository.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(NewCustomer(id: 7));
            RegistrosInsertados(0);
            CustomerDto customerDto = NewCustomerDto("Contoso");

            //Act
            var response = await _customerAppUoW.UpdateAsync(7, customerDto, CancellationToken.None);

            //Assert
            Assert.False(response.IsSuccess);
            Assert.False(response.Data);
        }

        [Fact]
        public async Task DeleteAsync_BorraLaEntidadRecuperadaYDevuelveExito()
        {
            //Arrange
            Customer existente = NewCustomer(id: 7);
            _repository.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(existente);
            RegistrosInsertados(1);

            //Act
            var response = await _customerAppUoW.DeleteAsync(7);

            //Assert: se borra el cliente que devolvio el repositorio.
            Assert.True(response.IsSuccess);
            Assert.True(response.Data);
            _repository.Received(1).Delete(existente);
        }

        [Fact]
        public async Task DeleteAsync_DevuelveNotFoundCuandoElClienteNoExiste()
        {
            //Arrange
            _repository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Customer?)null);

            //Act
            var response = await _customerAppUoW.DeleteAsync(99);

            //Assert
            Assert.False(response.IsSuccess);
            Assert.Equal(ErrorType.NotFound, response.ErrorType);
            _repository.DidNotReceive().Delete(Arg.Any<Customer>());
            await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }
    }
}
