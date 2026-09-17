using AutoMapper;
using Ecommerce.Application.Feature.Customers.Commands.CreateCustomerCommand;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interface.IRepository;
using FluentValidation;
using NSubstitute;
using NSubstitute.Core;
using Ecommerce.Transversal.Common.Enums;

namespace Ecommerce.Test.ApplicationTest.Feature.v3.Commands.CreateCustomer
{
    //Tests del handler v3: se sustituyen los limites (UnitOfWork y repositorio),
    //y el mapper y el validador se usan REALES porque forman parte del caso de uso.
    public class CreateCustomerTests : ApplicationTestBase
    {
        private readonly ICustomerRepositoryUoW _repository = Substitute.For<ICustomerRepositoryUoW>();
        private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
        private readonly IValidator<CreateCustomerCommand> _validator = new CreateCustomerValidator();
        private readonly IMapper _mapper = Substitute.For<IMapper>();
        private readonly CreateCustomerCommandHandle _handler;

        public CreateCustomerTests()
        {
            //El handler llega al repositorio a traves del UnitOfWork: la propiedad tiene que devolver nuestro doble.
            _unitOfWork._customersUoW.Returns(_repository);
            _handler = new CreateCustomerCommandHandle(_unitOfWork, Mapper, _validator);
        }



        [Fact]
        public async Task Handle_CreateCustomer_Ok()
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
            var mapeo = _mapper.Map<Customer>(request);
            _repository.CompareInfoInDb(mapeo, Arg.Any<CancellationToken>()).Returns(false);

            //Aqui guardaremos el Customer que el handler le pasa al repositorio.
            //Empieza vacio: se rellena durante el Act, cuando el handler llama a AddAsync.
            Customer? customerGuardado = null;

            //Accion que queremos ejecutar en cada llamada a AddAsync:
            //sacar el Customer de los argumentos de la llamada y guardarlo en customerGuardado.
            //"llamada" la rellena NSubstitute con los datos de la llamada real a AddAsync (sus argumentos).
            void guardarCustomer(CallInfo llamada)
            {
                var customerRecibido = llamada.Arg<Customer>();
                customerGuardado = customerRecibido;
            }

            //Le decimos al repositorio falso: "cuando te llamen a AddAsync, ejecuta guardarCustomer".
            _repository
                .When(repo => repo.AddAsync(Arg.Any<Customer>(), Arg.Any<CancellationToken>()))
                .Do(guardarCustomer);

            //El commit escribe una fila.
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

            //Act
            var response = await _handler.Handle(request, CancellationToken.None);

            //Assert: la respuesta es correcta.
            Assert.True(response.IsSuccess);
            Assert.True(response.Data);
            Assert.NotNull(customerGuardado);
            Assert.Null(customerGuardado.Id);
            Assert.Equal("Test", customerGuardado.CompanyName);

            //Assert: se confirmo el cambio una sola vez.
            //ReceivedCalls devuelve todas las llamadas que recibio el doble; nos quedamos con las de SaveChangesAsync y las contamos.
            var llamadasAlUnitOfWork = _unitOfWork.ReceivedCalls();
            var llamadasSaveChanges = llamadasAlUnitOfWork.Where(llamada => llamada.GetMethodInfo().Name == nameof(IUnitOfWork.SaveChangesAsync));
            var vecesSaveChanges = llamadasSaveChanges.Count();
            Assert.Equal(1, vecesSaveChanges);
        }

        [Fact]
        public async Task Handle_CreateCustomer_ValidationFail()
        {
            //Arrange: CompanyName vacio, el validador real tiene que rechazarlo.
            var request = new CreateCustomerCommand
            {
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
            //ReceivedCalls devuelve todas las llamadas que recibio el doble; filtramos por metodo y contamos.
            var llamadasAlRepositorio = _repository.ReceivedCalls();

            var llamadasCompareInfo = llamadasAlRepositorio.Where(llamada => llamada.GetMethodInfo().Name == nameof(ICustomerRepositoryUoW.CompareInfoInDb));
            var vecesCompareInfo = llamadasCompareInfo.Count();
            Assert.Equal(0, vecesCompareInfo);

            var llamadasAddAsync = llamadasAlRepositorio.Where(llamada => llamada.GetMethodInfo().Name == nameof(ICustomerRepositoryUoW.AddAsync));
            var vecesAddAsync = llamadasAddAsync.Count();
            Assert.Equal(0, vecesAddAsync);

            var llamadasAlUnitOfWork = _unitOfWork.ReceivedCalls();
            var llamadasSaveChanges = llamadasAlUnitOfWork.Where(llamada => llamada.GetMethodInfo().Name == nameof(IUnitOfWork.SaveChangesAsync));
            var vecesSaveChanges = llamadasSaveChanges.Count();
            Assert.Equal(0, vecesSaveChanges);
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
            //ReceivedCalls devuelve todas las llamadas que recibio el doble; filtramos por metodo y contamos.
            var llamadasAlRepositorio = _repository.ReceivedCalls();
            var llamadasAddAsync = llamadasAlRepositorio.Where(llamada => llamada.GetMethodInfo().Name == nameof(ICustomerRepositoryUoW.AddAsync));
            var vecesAddAsync = llamadasAddAsync.Count();
            Assert.Equal(0, vecesAddAsync);

            var llamadasAlUnitOfWork = _unitOfWork.ReceivedCalls();
            var llamadasSaveChanges = llamadasAlUnitOfWork.Where(llamada => llamada.GetMethodInfo().Name == nameof(IUnitOfWork.SaveChangesAsync));
            var vecesSaveChanges = llamadasSaveChanges.Count();
            Assert.Equal(0, vecesSaveChanges);
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
            //ReceivedCalls devuelve todas las llamadas que recibio el doble; filtramos por metodo y contamos.
            var llamadasAlRepositorio = _repository.ReceivedCalls();
            var llamadasAddAsync = llamadasAlRepositorio.Where(llamada => llamada.GetMethodInfo().Name == nameof(ICustomerRepositoryUoW.AddAsync));
            var vecesAddAsync = llamadasAddAsync.Count();
            Assert.Equal(1, vecesAddAsync);

            var llamadasAlUnitOfWork = _unitOfWork.ReceivedCalls();
            var llamadasSaveChanges = llamadasAlUnitOfWork.Where(llamada => llamada.GetMethodInfo().Name == nameof(IUnitOfWork.SaveChangesAsync));
            var vecesSaveChanges = llamadasSaveChanges.Count();
            Assert.Equal(1, vecesSaveChanges);
        }
    }
}
