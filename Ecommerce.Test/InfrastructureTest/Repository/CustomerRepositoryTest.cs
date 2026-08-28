using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Data;
using Ecommerce.Infrastructure.Interceptors;
using Ecommerce.Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Ecommerce.Test.InfrastructureTest.Repository
{
    public class CustomerRepositoryTest
    {
        //DbContextEF hereda de DbContext y no expone miembros virtuales: no se puede sustituir con NSubstitute.
        //Se usa el proveedor InMemory de EF Core, que da un contexto real y aislado por test.
        //El parametro dbName permite compartir el almacen entre dos contextos distintos: se escribe con uno
        //y se lee con otro, de modo que la lectura no venga del change tracker sino del almacen.
        private static DbContextEF CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<DbContextEF>()
                .UseInMemoryDatabase(dbName)
                .Options;

            //IConfiguration y el interceptor son andamiaje: el constructor de DbContextEF los exige
            //(lee la cadena de conexion) aunque con InMemory no se usen para nada.
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:EcommerceDb"] = "no-usada-en-tests"
                })
                .Build();

            return new DbContextEF(options, configuration, new AuditableEntitySaveChangesInterceptor());
        }

        private static string NewDbName() => Guid.NewGuid().ToString();

        private static Customer NewCustomer(string companyName = "Test") => new()
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

        [Fact]
        public async Task AddAsync_PersisteElClienteEnElAlmacen()
        {
            //Arrange
            var dbName = NewDbName();
            var customer = NewCustomer("Contoso");

            //Act
            bool result;
            await using (var writeContext = CreateContext(dbName))
            {
                result = await new CustomerRepository(writeContext).AddAsync(customer);
            }

            //Assert: se lee con un contexto nuevo, sin change tracker heredado
            await using var readContext = CreateContext(dbName);
            var saved = Assert.Single(readContext.Customers);

            Assert.True(result);
            Assert.NotSame(customer, saved);
            Assert.Equal("Contoso", saved.CompanyName);
            Assert.Equal("Test", saved.City);
        }

        [Fact]
        public async Task AddAsync_RellenaLosCamposDeAuditoria()
        {
            //Arrange
            var dbName = NewDbName();

            //Act
            await using (var writeContext = CreateContext(dbName))
            {
                await new CustomerRepository(writeContext).AddAsync(NewCustomer());
            }

            //Assert: lo escribe AuditableEntitySaveChangesInterceptor al interceptar el guardado
            await using var readContext = CreateContext(dbName);
            var saved = Assert.Single(readContext.Customers);

            Assert.Equal("System", saved.CreatedBy);
            Assert.Equal("System", saved.LastUpdatedBy);
            Assert.NotEqual(default, saved.CreatedAt);
        }

        [Fact]
        public async Task GetByIdAsync_DevuelveElClienteGuardado()
        {
            //Arrange
            var dbName = NewDbName();
            var customer = NewCustomer("Northwind");
            await using (var writeContext = CreateContext(dbName))
            {
                await new CustomerRepository(writeContext).AddAsync(customer);
            }

            //Act
            await using var readContext = CreateContext(dbName);
            var found = await new CustomerRepository(readContext).GetByIdAsync(customer.Id!.Value);

            //Assert
            Assert.NotNull(found);
            Assert.Equal("Northwind", found!.CompanyName);
        }

        [Fact]
        public async Task GetByIdAsync_DevuelveNullSiElClienteNoExiste()
        {
            //Arrange
            await using var context = CreateContext(NewDbName());
            var repository = new CustomerRepository(context);

            //Act
            var found = await repository.GetByIdAsync(999);

            //Assert
            Assert.Null(found);
        }

        [Fact]
        public async Task GetAllAsync_DevuelveTodosLosClientes()
        {
            //Arrange
            var dbName = NewDbName();
            await using (var writeContext = CreateContext(dbName))
            {
                var repository = new CustomerRepository(writeContext);
                await repository.AddAsync(NewCustomer("Uno"));
                await repository.AddAsync(NewCustomer("Dos"));
            }

            //Act
            await using var readContext = CreateContext(dbName);
            var result = await new CustomerRepository(readContext).GetAllAsync();

            //Assert
            Assert.Equal(2, result.Count());
            Assert.Contains(result, c => c.CompanyName == "Uno");
            Assert.Contains(result, c => c.CompanyName == "Dos");
        }

        [Fact]
        public async Task GetAllAsync_DevuelveColeccionVaciaSiNoHayClientes()
        {
            //Arrange
            await using var context = CreateContext(NewDbName());

            //Act
            var result = await new CustomerRepository(context).GetAllAsync();

            //Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task UpdateAsync_ModificaElClienteTrackeado()
        {
            //Arrange
            var dbName = NewDbName();
            var customer = NewCustomer("Original");
            await using (var writeContext = CreateContext(dbName))
            {
                await new CustomerRepository(writeContext).AddAsync(customer);
            }

            //Act: se recarga en un contexto nuevo, se modifica y se actualiza (patron connected)
            bool result;
            await using (var updateContext = CreateContext(dbName))
            {
                var repository = new CustomerRepository(updateContext);
                var tracked = await repository.GetByIdAsync(customer.Id!.Value);
                tracked!.CompanyName = "Modificado";
                result = await repository.UpdateAsync(tracked);
            }

            //Assert
            await using var readContext = CreateContext(dbName);
            var saved = Assert.Single(readContext.Customers);

            Assert.True(result);
            Assert.Equal("Modificado", saved.CompanyName);
            Assert.Equal("System", saved.LastUpdatedBy);
        }

        [Fact]
        public async Task UpdateAsync_ModificaElClienteDetached()
        {
            //Arrange
            var dbName = NewDbName();
            var customer = NewCustomer("Original");
            await using (var writeContext = CreateContext(dbName))
            {
                await new CustomerRepository(writeContext).AddAsync(customer);
            }

            //Act: entidad construida fuera del contexto, por lo que entra como Detached
            var detached = NewCustomer("Detached");
            detached.Id = customer.Id;

            await using (var updateContext = CreateContext(dbName))
            {
                var result = await new CustomerRepository(updateContext).UpdateAsync(detached);
                Assert.True(result);
            }

            //Assert
            await using var readContext = CreateContext(dbName);
            var saved = Assert.Single(readContext.Customers);
            Assert.Equal("Detached", saved.CompanyName);
        }

        [Fact]
        public async Task UpdateAsync_DevuelveFalseSiNoHayCambiosPendientes()
        {
            //Arrange
            var dbName = NewDbName();
            var customer = NewCustomer();
            await using (var writeContext = CreateContext(dbName))
            {
                await new CustomerRepository(writeContext).AddAsync(customer);
            }

            //Act: se recarga sin tocar ninguna propiedad, asi que SaveChangesAsync afecta a 0 filas
            await using var updateContext = CreateContext(dbName);
            var repository = new CustomerRepository(updateContext);
            var tracked = await repository.GetByIdAsync(customer.Id!.Value);
            var result = await repository.UpdateAsync(tracked!);

            //Assert: comportamiento actual, el bool refleja filas afectadas y no si la operacion fue valida
            Assert.False(result);
        }

        [Fact]
        public async Task DeleteAsync_EliminaElClienteExistente()
        {
            //Arrange
            var dbName = NewDbName();
            var customer = NewCustomer();
            await using (var writeContext = CreateContext(dbName))
            {
                await new CustomerRepository(writeContext).AddAsync(customer);
            }

            //Act
            bool result;
            await using (var deleteContext = CreateContext(dbName))
            {
                result = await new CustomerRepository(deleteContext).DeleteAsync(customer.Id!.Value);
            }

            //Assert
            await using var readContext = CreateContext(dbName);

            Assert.True(result);
            Assert.Empty(readContext.Customers);
        }

        [Fact]
        public async Task DeleteAsync_DevuelveFalseSiElClienteNoExiste()
        {
            //Arrange
            await using var context = CreateContext(NewDbName());

            //Act
            var result = await new CustomerRepository(context).DeleteAsync(999);

            //Assert
            Assert.False(result);
        }
    }
}
