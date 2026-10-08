using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Data;
using Ecommerce.Infrastructure.Interceptors;
using Ecommerce.Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using System.Text;
using System.Text.Json;

namespace Ecommerce.Test.InfrastructureTest.Repository
{
    //DbContext real con InMemory; la cache (IDistributedCache) y la configuracion se sustituyen.
    public class CustomerReadRepositoryTest
    {
        private readonly IDistributedCache _distributedCache = Substitute.For<IDistributedCache>();
        private readonly IConfiguration _configuration = Substitute.For<IConfiguration>();

        //DbContext real con InMemory (no se puede sustituir). dbName permite escribir con un contexto y leer con otro.
        private static DbContextEF CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<DbContextEF>()
                .UseInMemoryDatabase(dbName)
                .Options;

            //IConfiguration y el interceptor los exige el constructor, aunque con InMemory no se usan.
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:EcommerceDb"] = "no-usada-en-tests"
                })
                .Build();

            return new DbContextEF(options, configuration, new AuditableEntitySaveChangesInterceptor());
        }

        private static string NewDbName()
        {
            var dbName = Guid.NewGuid().ToString();
            return dbName;
        }

        private static Customer NewCustomer(string companyName = "Test")
        {
            var customer = new Customer
            {
                CompanyName = companyName,
                ContactName = "Test",
                ContactTitle = "Test",
                Address = "Test",
                City = "Test",
                Region = "Test",
                PostalCode = "Test",
                Country = "Test",
                Phone = "Test",
                Fax = "Test",
            };
            return customer;
        }

        //Guarda un cliente con su propio contexto.
        private static async Task AddCustomerAsync(string dbName, Customer customer)
        {
            await using var context = CreateContext(dbName);
            context.Customers.Add(customer);
            await context.SaveChangesAsync();
        }

        [Fact]
        public async Task GetByIdAsync_DevuelveElClienteGuardado()
        {
            //Arrange
            var dbName = NewDbName();
            var customer = NewCustomer("Peter");
            await AddCustomerAsync(dbName, customer);

            //Act: contexto nuevo, asi que FindAsync tiene que ir al almacen.
            await using var readContext = CreateContext(dbName);
            var repository = new CustomerReadRepository(readContext, _distributedCache, _configuration);
            var found = await repository.GetByIdAsync(customer.Id!.Value);

            //Assert
            Assert.NotNull(found);
            Assert.NotSame(customer, found);
            Assert.Equal("Peter", found!.CompanyName);
        }

        [Fact]
        public async Task GetByIdAsync_DevuelveNullSiElClienteNoExiste()
        {
            //Arrange: un solo contexto, no hay nada escrito que leer de vuelta.
            await using var context = CreateContext(NewDbName());
            var repository = new CustomerReadRepository(context, _distributedCache, _configuration);

            //Act
            var found = await repository.GetByIdAsync(999);

            //Assert
            Assert.Null(found);
        }

        [Fact]
        public async Task GetAllAsync_ConDatosEnCache_DevuelveLosDeLaCacheYNoConsultaLaBaseDeDatos()
        {
            //Arrange: la cache y la base de datos tienen clientes distintos para saber de donde sale el resultado.
            var dbName = NewDbName();
            await AddCustomerAsync(dbName, NewCustomer("DesdeBd"));

            var customersInCache = new List<Customer> { NewCustomer("DesdeCache") };
            var cachedBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(customersInCache));
            _distributedCache.GetAsync("CustomerAll", Arg.Any<CancellationToken>()).Returns(cachedBytes);

            //Act
            await using var readContext = CreateContext(dbName);
            var repository = new CustomerReadRepository(readContext, _distributedCache, _configuration);
            var result = await repository.GetAllAsync();

            //Assert: viene de la cache y no se vuelve a guardar en ella.
            var customer = Assert.Single(result);
            Assert.Equal("DesdeCache", customer.CompanyName);
            await _distributedCache.DidNotReceive().SetAsync("CustomerAll", Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GetAllAsync_SinDatosEnCache_LeeDeLaBaseDeDatosYLaGuardaEnCache()
        {
            //Arrange: la cache devuelve null (no hay nada guardado) y la configuracion tiene las caducidades de CustomerAll.
            var dbName = NewDbName();
            await AddCustomerAsync(dbName, NewCustomer("DesdeBd"));

            _distributedCache.GetAsync("CustomerAll", Arg.Any<CancellationToken>()).Returns(Task.FromResult<byte[]?>(null));
            _configuration["Cache:Policies:CustomerAll:AbsoluteExpiration"].Returns("01:00:00");
            _configuration["Cache:Policies:CustomerAll:SlidingExpiration"].Returns("00:12:00");

            //Act
            await using var readContext = CreateContext(dbName);
            var repository = new CustomerReadRepository(readContext, _distributedCache, _configuration);
            var result = await repository.GetAllAsync();

            //Assert: el resultado sale de la base de datos y se guarda una vez en la cache.
            var customer = Assert.Single(result);
            Assert.Equal("DesdeBd", customer.CompanyName);
            await _distributedCache.Received(1).SetAsync("CustomerAll", Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GetAllAsync_SinDatosEnCache_GuardaLosClientesSerializadosYConLasCaducidadesConfiguradas()
        {
            //Arrange
            var dbName = NewDbName();
            await AddCustomerAsync(dbName, NewCustomer("DesdeBd"));

            _distributedCache.GetAsync("CustomerAll", Arg.Any<CancellationToken>()).Returns(Task.FromResult<byte[]?>(null));
            _configuration["Cache:Policies:CustomerAll:AbsoluteExpiration"].Returns("01:00:00");
            _configuration["Cache:Policies:CustomerAll:SlidingExpiration"].Returns("00:12:00");

            //Act
            await using var readContext = CreateContext(dbName);
            var repository = new CustomerReadRepository(readContext, _distributedCache, _configuration);
            await repository.GetAllAsync();

            //Assert: se busca la llamada a SetAsync entre las que recibio la cache y se leen sus argumentos.
            var llamadas = _distributedCache.ReceivedCalls().ToList();
            var llamadaSet = llamadas.Single(c => c.GetMethodInfo().Name == "SetAsync");
            var argumentos = llamadaSet.GetArguments();

            var bytesGuardados = (byte[])argumentos[1]!;
            var textoGuardado = Encoding.UTF8.GetString(bytesGuardados);
            var clientesGuardados = JsonSerializer.Deserialize<List<Customer>>(textoGuardado);
            var clienteGuardado = Assert.Single(clientesGuardados!);
            Assert.Equal("DesdeBd", clienteGuardado.CompanyName);

            var opciones = (DistributedCacheEntryOptions)argumentos[2]!;
            Assert.Equal(TimeSpan.FromHours(1), opciones.AbsoluteExpirationRelativeToNow);
            Assert.Equal(TimeSpan.FromMinutes(12), opciones.SlidingExpiration);
        }

        [Fact]
        public async Task GetAllAsync_SinDatosEnCacheNiClientes_DevuelveColeccionVacia()
        {
            //Arrange: la base de datos esta vacia y la cache no tiene nada.
            _distributedCache.GetAsync("CustomerAll", Arg.Any<CancellationToken>()).Returns(Task.FromResult<byte[]?>(null));
            _configuration["Cache:Policies:CustomerAll:AbsoluteExpiration"].Returns("01:00:00");
            _configuration["Cache:Policies:CustomerAll:SlidingExpiration"].Returns("00:12:00");

            await using var context = CreateContext(NewDbName());
            var repository = new CustomerReadRepository(context, _distributedCache, _configuration);

            //Act
            var result = await repository.GetAllAsync();

            //Assert
            Assert.Empty(result);
        }
    }
}
