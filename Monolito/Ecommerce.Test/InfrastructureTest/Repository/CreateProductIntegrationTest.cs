using Ecommerce.Application.Feature.Products.Commands.CreateProduct;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Enum;
using Ecommerce.Domain.Interface.IRepository.Jwt;
using Ecommerce.Infrastructure.Data;
using Ecommerce.Infrastructure.Interceptors;
using Ecommerce.Infrastructure.Repository;
using Ecommerce.Test.ApplicationTest;
using Ecommerce.Transversal.Common;
using Ecommerce.Transversal.Common.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NSubstitute;

namespace Ecommerce.Test.InfrastructureTest.Repository
{
    //Handler real + repositorios reales + EF InMemory: comprueba que el producto se guarda y que el duplicado se detecta contra la base de datos.
    public class CreateProductIntegrationTest : ApplicationTestBase
    {
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

        //Un UnitOfWork nuevo con el contexto indicado: equivale a una peticion HTTP (DbContext scoped).
        private static CreateProductCommandHandle CreateHandler(DbContextEF context)
        {
            var customerRepository = new CustomerRepositoryUoW(context);
            var userRepository = Substitute.For<IUserRepository>();
            var productRepository = new ProductRepository(context);
            var unitOfWork = new UnitOfWork(context, customerRepository, userRepository, productRepository);
            return new CreateProductCommandHandle(unitOfWork, Mapper);
        }

        [Fact]
        public async Task Handle_PersisteElProductoConTodosSusCampos()
        {
            //Arrange
            var dbName = NewDbName();
            var request = new CreateProductCommand
            {
                Name = "Portatil",
                Description = "Ultrabook",
                Price = 999.99m,
                StockQuantity = 3,
                Category = 2
            };

            //Act: contexto nuevo, como una peticion nueva.
            Response<bool> response;
            await using (var requestContext = CreateContext(dbName))
            {
                var handler = CreateHandler(requestContext);
                response = await handler.Handle(request, CancellationToken.None);
            }

            //Assert: contexto nuevo, el producto viene del almacen con un Id generado.
            await using var readContext = CreateContext(dbName);
            var products = readContext.Products.ToList();
            var numeroDeProductos = products.Count;
            Assert.Equal(1, numeroDeProductos);

            var saved = products[0];
            Assert.True(response.IsSuccess);
            Assert.NotNull(saved.Id);
            Assert.Equal("Portatil", saved.Name);
            Assert.Equal("Ultrabook", saved.Description);
            Assert.Equal(999.99m, saved.Price);
            Assert.Equal(3, saved.StockQuantity);
            Assert.Equal(Category.Electronics, saved.Category);
        }

        [Fact]
        public async Task Handle_RellenaLosCamposDeAuditoria()
        {
            //Arrange
            var dbName = NewDbName();
            var request = new CreateProductCommand
            {
                Name = "Libro",
                Description = "Novela",
                Price = 10.5m,
                StockQuantity = 5,
                Category = 1
            };

            //Act: el interceptor se dispara en el SaveChangesAsync del UnitOfWork.
            await using (var requestContext = CreateContext(dbName))
            {
                var handler = CreateHandler(requestContext);
                await handler.Handle(request, CancellationToken.None);
            }

            //Assert: contexto nuevo, asi que la auditoria se lee del almacen.
            await using var readContext = CreateContext(dbName);
            var products = readContext.Products.ToList();
            var saved = products[0];

            Assert.Equal("System", saved.CreatedBy);
            Assert.Equal("System", saved.LastUpdatedBy);
            Assert.NotEqual(default, saved.CreatedAt);
        }

        [Fact]
        public async Task Handle_MismoProductoDosVeces_LaSegundaEsDuplicada()
        {
            //Arrange: la misma peticion enviada dos veces.
            var dbName = NewDbName();
            var request = new CreateProductCommand
            {
                Name = "Libro",
                Description = "Novela",
                Price = 10.5m,
                StockQuantity = 5,
                Category = 1
            };

            //Act: cada llamada con su contexto, como dos peticiones distintas.
            Response<bool> primeraRespuesta;
            await using (var primerContexto = CreateContext(dbName))
            {
                var handler = CreateHandler(primerContexto);
                primeraRespuesta = await handler.Handle(request, CancellationToken.None);
            }

            Response<bool> segundaRespuesta;
            await using (var segundoContexto = CreateContext(dbName))
            {
                var handler = CreateHandler(segundoContexto);
                segundaRespuesta = await handler.Handle(request, CancellationToken.None);
            }

            //Assert: la primera se guarda, la segunda es duplicada y solo hay un producto en el almacen.
            await using var readContext = CreateContext(dbName);
            var products = readContext.Products.ToList();
            var numeroDeProductos = products.Count;

            Assert.True(primeraRespuesta.IsSuccess);
            Assert.False(segundaRespuesta.IsSuccess);
            Assert.Equal(ErrorType.Duplicated, segundaRespuesta.ErrorType);
            Assert.Equal(1, numeroDeProductos);
        }

        [Fact]
        public async Task Handle_ProductosConDistintoPrecio_NoSonDuplicados()
        {
            //Arrange: mismo nombre y descripcion pero distinto precio.
            var dbName = NewDbName();
            var primeraPeticion = new CreateProductCommand
            {
                Name = "Libro",
                Description = "Novela",
                Price = 10.5m,
                StockQuantity = 5,
                Category = 1
            };
            var segundaPeticion = new CreateProductCommand
            {
                Name = "Libro",
                Description = "Novela",
                Price = 12m,
                StockQuantity = 5,
                Category = 1
            };

            //Act
            await using (var primerContexto = CreateContext(dbName))
            {
                var handler = CreateHandler(primerContexto);
                await handler.Handle(primeraPeticion, CancellationToken.None);
            }

            Response<bool> segundaRespuesta;
            await using (var segundoContexto = CreateContext(dbName))
            {
                var handler = CreateHandler(segundoContexto);
                segundaRespuesta = await handler.Handle(segundaPeticion, CancellationToken.None);
            }

            //Assert: los dos productos estan en el almacen.
            await using var readContext = CreateContext(dbName);
            var products = readContext.Products.ToList();
            var numeroDeProductos = products.Count;

            Assert.True(segundaRespuesta.IsSuccess);
            Assert.Equal(2, numeroDeProductos);
        }
    }
}
