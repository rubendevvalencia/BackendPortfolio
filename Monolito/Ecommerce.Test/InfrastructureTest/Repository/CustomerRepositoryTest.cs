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

            //Assert: contexto nuevo, asi que el cliente viene del almacen y no es la misma instancia.
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

            //Assert: contexto nuevo, asi que la auditoria se lee del almacen.
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

            //Act: contexto nuevo, asi que FindAsync tiene que ir al almacen.
            await using var readContext = CreateContext(dbName);
            var found = await new CustomerRepository(readContext).GetByIdAsync(customer.Id!.Value);

            //Assert
            Assert.NotNull(found);
            Assert.NotSame(customer, found);
            Assert.Equal("Northwind", found!.CompanyName);
        }

        [Fact]
        public async Task GetByIdAsync_DevuelveNullSiElClienteNoExiste()
        {
            //Arrange: un solo contexto, no hay nada escrito que leer de vuelta
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

            //Act: contexto nuevo, asi que la consulta lee del almacen.
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
            //Arrange: un solo contexto, no hay nada escrito que leer de vuelta
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

            //Act: contexto nuevo, se recarga desde el almacen y se modifica ya trackeado (patron connected)
            bool result;
            await using (var updateContext = CreateContext(dbName))
            {
                var repository = new CustomerRepository(updateContext);
                var tracked = await repository.GetByIdAsync(customer.Id!.Value);
                tracked!.CompanyName = "Modificado";
                result = await repository.UpdateAsync(tracked);
            }

            //Assert: contexto nuevo para comprobar que el cambio se guardo de verdad.
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

            //Act: entidad creada fuera y contexto nuevo, asi que entra como Detached.
            var detached = NewCustomer("Detached");
            detached.Id = customer.Id;

            bool result;
            await using (var updateContext = CreateContext(dbName))
            {
                result = await new CustomerRepository(updateContext).UpdateAsync(detached);
            }

            //Assert: contexto nuevo para comprobar que el cambio se guardo de verdad.
            await using var readContext = CreateContext(dbName);
            var saved = Assert.Single(readContext.Customers);

            Assert.True(result);
            Assert.NotSame(detached, saved);
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

            //Act: contexto nuevo y sin cambios, asi que SaveChangesAsync afecta a 0 filas.
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

            bool resultAdd;
            await using (var writeContext = CreateContext(dbName))
            {
                resultAdd = await new CustomerRepository(writeContext).AddAsync(customer);
            }

            //Act: contexto nuevo, asi que DeleteAsync tiene que localizar el cliente en el almacen
            bool resultDelete;
            Customer? resultFind;
            await using (var deleteContext = CreateContext(dbName))
            {
                var repository = new CustomerRepository(deleteContext);
                resultDelete = await repository.DeleteAsync(customer.Id!.Value);
                resultFind = await repository.GetByIdAsync(customer.Id!.Value);
            }

            //Assert: contexto nuevo para comprobar el resultado en el almacen.
            await using var readContext = CreateContext(dbName);

            Assert.True(resultAdd);
            Assert.True(resultDelete);
            Assert.Null(resultFind);
            Assert.Empty(readContext.Customers);
        }

        [Fact]
        public async Task DeleteAsync_DevuelveFalseSiElClienteNoExiste()
        {
            //Arrange: un solo contexto, no hay nada escrito que borrar
            await using var context = CreateContext(NewDbName());

            //Act
            var result = await new CustomerRepository(context).DeleteAsync(999);

            //Assert
            Assert.False(result);
        }
    }
}
