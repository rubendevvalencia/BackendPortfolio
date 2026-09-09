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
    //Tests de la capa Application: se sustituyen los limites (UnitOfWork y repositorio),
    //y el mapper y el validador se usan REALES porque forman parte del caso de uso.
    public class CustomerApplicationUoWTest : ApplicationTestBase
    {
        private readonly ICustomerRepositoryUoW _repository = Substitute.For<ICustomerRepositoryUoW>();
        private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
        private readonly CustomerApplicationUoW _customerAppUoW;

        //xUnit crea una instancia de la clase por cada [Fact], asi que los dobles
        //nacen limpios en cada test y no hay estado compartido entre ellos.
        public CustomerApplicationUoWTest()
        {
            //El caso de uso llega al repositorio a traves del UnitOfWork:
            //la propiedad tiene que devolver nuestro doble.
            _unitOfWork._customersUoW.Returns(_repository);
            _customerAppUoW = new CustomerApplicationUoW(_unitOfWork, Mapper, CustomerValidator);
        }

        //Simula el resultado del commit: cuantas filas dice EF que ha escrito.
        //ReturnsForAnyArgs ignora los argumentos, asi que sobra el Arg.Any del token.
        private ConfiguredCall RegistrosInsertados(int filas)
        {
           return _unitOfWork.SaveChangesAsync().ReturnsForAnyArgs(filas);
        } 

        // ---------- AddAsync ----------

        [Fact]
        public async Task AddAsync_DevuelveExitoCuandoElCommitEscribe()
        {
            //Arrange: se captura el Customer que recibe el repositorio para mirarlo despues.
            Customer? customerGuardado = null;
            var tarea = _repository.AddAsync(Arg.Do<Customer>(c => customerGuardado = c), Arg.Any<CancellationToken>());
            RegistrosInsertados(1);

            CustomerDto customerDto = NewCustomerDto("Contoso");

            //Act
            var response = await _customerAppUoW.AddAsync(customerDto, CancellationToken.None);

            //Assert: ademas del Response se comprueba que el caso de uso forzo el Id a null,
            //porque lo genera la base de datos y ese detalle solo se ve aqui.
            Assert.True(response.IsSuccess);
            Assert.True(response.Data);
            Assert.NotNull(customerGuardado);
            Assert.Null(customerGuardado.Id);
            Assert.Equal("Contoso", customerGuardado.CompanyName);
        }

        [Fact]
        public async Task AddAsync_NoTocaLaPersistenciaCuandoElDtoNoEsValido()
        {
            //Arrange: CompanyName vacio incumple la regla NotEmpty de CustomerDtoValidator.
            CustomerDto customerDto = NewCustomerDto(string.Empty);

            //Act
            var response = await _customerAppUoW.AddAsync(customerDto, CancellationToken.None);

            //Assert: la validacion corta el caso de uso antes de llegar al repositorio.
            Assert.False(response.IsSuccess);
            Assert.Equal(ErrorType.Validation, response.ErrorType);
            Assert.True(response.Errors.ContainsKey(nameof(CustomerDto.CompanyName)));
            await _repository.DidNotReceive().AddAsync(Arg.Any<Customer>(), Arg.Any<CancellationToken>());
            await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task AddAsync_DevuelveFalloCuandoLaPersistenciaLanzaExcepcion()
        {
            //Arrange: el commit revienta, como haria un fallo real de base de datos.
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
                       .Throws(new InvalidOperationException("fallo de base de datos"));
            CustomerDto customerDto = NewCustomerDto("Contoso");

            //Act
            var response = await _customerAppUoW.AddAsync(customerDto, CancellationToken.None);

            //Assert: el caso de uso no propaga la excepcion, la traduce a Response.
            Assert.False(response.IsSuccess);
            Assert.Equal(ErrorType.Unexpected, response.ErrorType);
            Assert.Equal("fallo de base de datos", response.Message);
        }

        // ---------- GetByIdAsync ----------

        [Fact]
        public async Task GetByIdAsync_DevuelveElDtoCuandoElClienteExiste()
        {
            //Arrange
            Customer customer = NewCustomer(id: 7, companyName: "Contoso");
            _repository.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(customer);

            //Act
            var response = await _customerAppUoW.GetByIdAsync(7);

            //Assert: se comprueba el mapeo real de entidad a DTO.
            //CustomerDto no expone Id (no se puede afirmar sobre el aqui).
            Assert.True(response.IsSuccess);
            Assert.NotNull(response.Data);
            Assert.Equal("Contoso", response.Data.CompanyName);
            Assert.Equal(customer.City, response.Data.City);
        }

        [Fact]
        public async Task GetByIdAsync_DevuelveNotFoundCuandoElClienteNoExiste()
        {
            //Arrange: el repositorio no encuentra nada.
            _repository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Customer?)null);

            //Act
            var response = await _customerAppUoW.GetByIdAsync(99);

            //Assert: el "no existe" es NotFound (404), no un fallo generico (500).
            Assert.False(response.IsSuccess);
            Assert.Equal(ErrorType.NotFound, response.ErrorType);
            Assert.Null(response.Data);
        }

        // ---------- GetAllAsync ----------

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
            //Arrange: sin clientes NO es un error, es una lista vacia.
            _repository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Customer>());

            //Act
            var response = await _customerAppUoW.GetAllAsync();

            //Assert
            Assert.True(response.IsSuccess);
            Assert.Empty(response.Data);
        }

        // ---------- UpdateAsync ----------

        [Fact]
        public async Task UpdateAsync_CopiaLosCamposSobreLaEntidadExistente()
        {
            //Arrange: la entidad viene trackeada del repositorio y se modifica en sitio.
            Customer existente = NewCustomer(id: 7, companyName: "Nombre viejo");
            _repository.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(existente);
            RegistrosInsertados(1);

            CustomerDto customerDto = NewCustomerDto("Nombre nuevo");

            //Act
            var response = await _customerAppUoW.UpdateAsync(7, customerDto, CancellationToken.None);

            //Assert: se actualiza la MISMA instancia (no una nueva) y se conserva el Id.
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

            //Assert: ni se marca la modificacion ni se confirma.
            Assert.False(response.IsSuccess);
            Assert.Equal(ErrorType.NotFound, response.ErrorType);
            _repository.DidNotReceive().Update(Arg.Any<Customer>());
            await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdateAsync_NoBuscaElClienteCuandoElDtoNoEsValido()
        {
            //Arrange: se valida ANTES de ir a la base de datos.
            CustomerDto customerDto = NewCustomerDto(string.Empty);

            //Act
            var response = await _customerAppUoW.UpdateAsync(7, customerDto, CancellationToken.None);

            //Assert
            Assert.False(response.IsSuccess);
            Assert.Equal(ErrorType.Validation, response.ErrorType);
            await _repository.DidNotReceive().GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
        }

        //TEST DE CARACTERIZACION DE UN DEFECTO: NO describe el comportamiento deseado.
        //Este es el pendiente #2 del README: el PUT idempotente (actualizar con los mismos datos)
        //hace que EF no escriba ninguna fila, SaveChangesAsync devuelve 0 y el caso de uso lo
        //traduce a error -> 500. Como la existencia ya se comprueba por separado, un 0 deberia
        //leerse como exito sin efecto y este test deberia afirmar IsSuccess == true.
        //Se deja fijado para que la deuda sea visible: cuando se arregle el pendiente #2 este
        //test se pondra rojo, y ese rojo es el arreglo, no una regresion. Actualizarlo entonces.
        [Fact]
        public async Task UpdateAsync_HoyDevuelveFalloConCeroFilas_PendienteDeCorregir()
        {
            //Arrange: el cliente existe y el commit no escribe nada, como en un PUT idempotente.
            _repository.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(NewCustomer(id: 7));
            RegistrosInsertados(0);
            CustomerDto customerDto = NewCustomerDto("Contoso");

            //Act
            var response = await _customerAppUoW.UpdateAsync(7, customerDto, CancellationToken.None);

            //Assert: comportamiento ACTUAL, defectuoso. Lo correcto seria exito sin efecto.
            Assert.False(response.IsSuccess);
            Assert.False(response.Data);
        }

        // ---------- DeleteAsync ----------

        [Fact]
        public async Task DeleteAsync_BorraLaEntidadRecuperadaYDevuelveExito()
        {
            //Arrange
            Customer existente = NewCustomer(id: 7);
            _repository.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(existente);
            RegistrosInsertados(1);

            //Act
            var response = await _customerAppUoW.DeleteAsync(7);

            //Assert: se borra exactamente la entidad que devolvio el repositorio.
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
