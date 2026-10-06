using Ecommerce.Application.Feature.ProductToCustomer;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Enum;
using Ecommerce.Domain.Interface.IRepository.Jwt;
using Ecommerce.Infrastructure.Data;
using Ecommerce.Infrastructure.Interceptors;
using Ecommerce.Infrastructure.Repository;
using Ecommerce.Transversal.Common;
using Ecommerce.Transversal.Common.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NSubstitute;

namespace Ecommerce.Test.InfrastructureTest.Repository
{
    //Handler real + repositorios reales + EF InMemory: comprueba que la relacion Customer-Product (tabla CustomerProducts) se guarda de verdad.
    public class ProductToCustomerIntegrationTest
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
                Fax = "Test"
            };
            return customer;
        }

        private static Product NewProduct(string name = "Test")
        {
            var product = new Product
            {
                Name = name,
                Description = "Test",
                Price = 10,
                StockQuantity = 5,
                Category = Category.Book
            };
            return product;
        }

        //Un UnitOfWork nuevo con el contexto indicado: equivale a una peticion HTTP (DbContext scoped).
        private static ProductToCustomerCommandHandle CreateHandler(DbContextEF context)
        {
            var customerRepository = new CustomerRepositoryUoW(context);
            var userRepository = Substitute.For<IUserRepository>();
            var productRepository = new ProductRepository(context);
            var unitOfWork = new UnitOfWork(context, customerRepository, userRepository, productRepository);
            return new ProductToCustomerCommandHandle(unitOfWork);
        }

        //Guarda clientes y productos con su propio contexto, sin asociarlos.
        private static async Task SeedAsync(string dbName, List<Customer> customers, List<Product> products)
        {
            await using var context = CreateContext(dbName);
            await context.Customers.AddRangeAsync(customers);
            await context.Products.AddRangeAsync(products);
            await context.SaveChangesAsync();
        }

        [Fact]
        public async Task Handle_AsociaElProductoYLoPersiste()
        {
            //Arrange
            var dbName = NewDbName();
            var customer = NewCustomer();
            var product = NewProduct("Libro");
            await SeedAsync(dbName, new List<Customer> { customer }, new List<Product> { product });

            var request = new ProductToCustomerCommand
            {
                CustomerId = customer.Id!.Value,
                ProductId = product.Id!.Value
            };

            //Act: contexto nuevo, como una peticion nueva.
            Response<bool> response;
            await using (var requestContext = CreateContext(dbName))
            {
                var handler = CreateHandler(requestContext);
                response = await handler.Handle(request, CancellationToken.None);
            }

            //Assert: contexto nuevo, el cliente tiene el producto en el almacen.
            await using var readContext = CreateContext(dbName);
            var saved = await readContext.Customers.Include(c => c.Products).SingleAsync();

            Assert.True(response.IsSuccess);
            Assert.True(response.Data);
            Assert.NotNull(saved.Products);
            var numeroDeProductos = saved.Products.Count;
            Assert.Equal(1, numeroDeProductos);
            var productoGuardado = saved.Products.First();
            Assert.Equal("Libro", productoGuardado.Name);
        }

        [Fact]
        public async Task Handle_ElMismoProductoEnDosClientes_LoVeLosDos()
        {
            //Arrange: la relacion es muchos a muchos, asi que un producto puede estar en varios clientes.
            var dbName = NewDbName();
            var primerCliente = NewCustomer("Uno");
            var segundoCliente = NewCustomer("Dos");
            var product = NewProduct("Compartido");
            await SeedAsync(dbName, new List<Customer> { primerCliente, segundoCliente }, new List<Product> { product });

            var primeraPeticion = new ProductToCustomerCommand
            {
                CustomerId = primerCliente.Id!.Value,
                ProductId = product.Id!.Value
            };
            var segundaPeticion = new ProductToCustomerCommand
            {
                CustomerId = segundoCliente.Id!.Value,
                ProductId = product.Id!.Value
            };

            //Act: cada llamada con su contexto, como dos peticiones distintas.
            await using (var primerContexto = CreateContext(dbName))
            {
                var handler = CreateHandler(primerContexto);
                await handler.Handle(primeraPeticion, CancellationToken.None);
            }
            await using (var segundoContexto = CreateContext(dbName))
            {
                var handler = CreateHandler(segundoContexto);
                await handler.Handle(segundaPeticion, CancellationToken.None);
            }

            //Assert: contexto nuevo, los dos clientes tienen el producto y el producto conoce a los dos.
            await using var readContext = CreateContext(dbName);
            var customers = await readContext.Customers.Include(c => c.Products).ToListAsync();
            var savedProduct = await readContext.Products.Include(p => p.Customer).SingleAsync();

            var numeroDeClientes = customers.Count;
            Assert.Equal(2, numeroDeClientes);

            var productosDelPrimero = customers[0].Products!.Count;
            var productosDelSegundo = customers[1].Products!.Count;
            var clientesDelProducto = savedProduct.Customer!.Count;
            Assert.Equal(1, productosDelPrimero);
            Assert.Equal(1, productosDelSegundo);
            Assert.Equal(2, clientesDelProducto);
        }

        [Fact]
        public async Task Handle_DosProductosDistintosEnElMismoCliente_AcumulaAmbos()
        {
            //Arrange
            var dbName = NewDbName();
            var customer = NewCustomer();
            var primero = NewProduct("Primero");
            var segundo = NewProduct("Segundo");
            await SeedAsync(dbName, new List<Customer> { customer }, new List<Product> { primero, segundo });

            var primeraPeticion = new ProductToCustomerCommand
            {
                CustomerId = customer.Id!.Value,
                ProductId = primero.Id!.Value
            };
            var segundaPeticion = new ProductToCustomerCommand
            {
                CustomerId = customer.Id!.Value,
                ProductId = segundo.Id!.Value
            };

            //Act: la segunda llamada carga el cliente sin Products (null) y aun asi no tiene que perder el primero.
            await using (var primerContexto = CreateContext(dbName))
            {
                var handler = CreateHandler(primerContexto);
                await handler.Handle(primeraPeticion, CancellationToken.None);
            }
            await using (var segundoContexto = CreateContext(dbName))
            {
                var handler = CreateHandler(segundoContexto);
                await handler.Handle(segundaPeticion, CancellationToken.None);
            }

            //Assert: contexto nuevo, el cliente tiene los dos productos.
            await using var readContext = CreateContext(dbName);
            var saved = await readContext.Customers.Include(c => c.Products).SingleAsync();

            var numeroDeProductos = saved.Products!.Count;
            Assert.Equal(2, numeroDeProductos);

            var productos = saved.Products.ToList();
            Assert.Equal("Primero", productos[0].Name);
            Assert.Equal("Segundo", productos[1].Name);
        }

        [Fact]
        public async Task Handle_ClienteInexistente_NoCreaNingunaAsociacion()
        {
            //Arrange: existe el producto pero no el cliente 999.
            var dbName = NewDbName();
            var customer = NewCustomer();
            var product = NewProduct();
            await SeedAsync(dbName, new List<Customer> { customer }, new List<Product> { product });

            var request = new ProductToCustomerCommand
            {
                CustomerId = 999,
                ProductId = product.Id!.Value
            };

            //Act
            Response<bool> response;
            await using (var requestContext = CreateContext(dbName))
            {
                var handler = CreateHandler(requestContext);
                response = await handler.Handle(request, CancellationToken.None);
            }

            //Assert: NotFound y el unico cliente que existe sigue sin productos.
            await using var readContext = CreateContext(dbName);
            var saved = await readContext.Customers.Include(c => c.Products).SingleAsync();

            Assert.False(response.IsSuccess);
            Assert.Equal(ErrorType.NotFound, response.ErrorType);
            Assert.Empty(saved.Products!);
        }

        [Fact]
        public async Task Handle_ProductoInexistente_NoCreaNingunaAsociacion()
        {
            //Arrange: existe el cliente pero no el producto 999.
            var dbName = NewDbName();
            var customer = NewCustomer();
            var product = NewProduct();
            await SeedAsync(dbName, new List<Customer> { customer }, new List<Product> { product });

            var request = new ProductToCustomerCommand
            {
                CustomerId = customer.Id!.Value,
                ProductId = 999
            };

            //Act
            Response<bool> response;
            await using (var requestContext = CreateContext(dbName))
            {
                var handler = CreateHandler(requestContext);
                response = await handler.Handle(request, CancellationToken.None);
            }

            //Assert: NotFound y el cliente sigue sin productos.
            await using var readContext = CreateContext(dbName);
            var saved = await readContext.Customers.Include(c => c.Products).SingleAsync();

            Assert.False(response.IsSuccess);
            Assert.Equal(ErrorType.NotFound, response.ErrorType);
            Assert.Empty(saved.Products!);
        }
    }
}
