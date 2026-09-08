using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interface.IRepository;
using Ecommerce.Domain.Interface.IRepository.Jwt;
using Ecommerce.Infrastructure.Data;
using Ecommerce.Infrastructure.Interceptors;
using Ecommerce.Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NSubstitute;

namespace Ecommerce.Test.InfrastructureTest.Repository
{
    public class CustomerRepositoryUoWTest
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

        //El repositorio y el UnitOfWork comparten la MISMA instancia de contexto, igual que hace la DI
        //en produccion con el DbContext scoped. Ese contexto compartido es lo que permite que el
        //SaveChangesAsync del UnitOfWork confirme lo que el repositorio ha ido marcando.
        //IUserRepository es andamiaje: el constructor lo exige pero ningun test de Customer lo ejerce.
        private static UnitOfWork CreateUnitOfWork(DbContextEF context)
        {
            var repository = new CustomerRepositoryUoW(context);
            var userRepository = Substitute.For<IUserRepository>();
            return new UnitOfWork(context, repository, userRepository);
        }

        //Deja un cliente confirmado en el almacen y devuelve el control con su Id ya generado.
        //Usa su propio contexto para que el Act arranque siempre con el change tracker vacio.
        private static async Task AddCustomerAsync(string dbName, Customer customer)
        {
            await using var context = CreateContext(dbName);
            var unitOfWork = CreateUnitOfWork(context);
            await unitOfWork._customersUoW.AddAsync(customer);
            await unitOfWork.SaveChangesAsync();
        }

        [Fact]
        public async Task AddAsync_PersisteElClienteEnElAlmacen()
        {
            //Arrange
            var dbName = NewDbName();
            var customer = NewCustomer("Contoso");

            //Act: el repositorio solo marca la entidad, la confirmacion la hace el UnitOfWork
            int rowsAffected;
            await using (var writeContext = CreateContext(dbName))
            {
                var unitOfWork = CreateUnitOfWork(writeContext);
                await unitOfWork._customersUoW.AddAsync(customer);
                rowsAffected = await unitOfWork.SaveChangesAsync();
            }

            //Assert: contexto nuevo, con el change tracker vacio. El cliente se materializa desde el
            //almacen, asi que NotSame demuestra que hubo persistencia real y no una instancia reutilizada
            await using var readContext = CreateContext(dbName);
            var saved = Assert.Single(readContext.Customers);

            Assert.Equal(1, rowsAffected);
            Assert.NotSame(customer, saved);
            Assert.Equal("Contoso", saved.CompanyName);
            Assert.Equal("Test", saved.City);
        }

        [Fact]
        public async Task AddAsync_SinSaveChangesNoPersisteNada()
        {
            //Arrange
            var dbName = NewDbName();
            var customer = NewCustomer();

            //Act: se anade y se cierra el contexto SIN confirmar. Es la garantia que aporta el patron:
            //sin el SaveChangesAsync del UnitOfWork, el trabajo del repositorio se descarta entero
            EntityState stateAfterAdd;
            await using (var writeContext = CreateContext(dbName))
            {
                var unitOfWork = CreateUnitOfWork(writeContext);
                await unitOfWork._customersUoW.AddAsync(customer);
                stateAfterAdd = writeContext.Entry(customer).State;
            }

            //Assert: contexto nuevo, asi que el almacen es la unica fuente de verdad
            await using var readContext = CreateContext(dbName);

            Assert.Equal(EntityState.Added, stateAfterAdd);
            Assert.Empty(readContext.Customers);
        }

        [Fact]
        public async Task AddAsync_ConfirmaVariosClientesEnUnUnicoSaveChanges()
        {
            //Arrange
            var dbName = NewDbName();

            //Act: dos operaciones acumuladas en el mismo contexto y una sola confirmacion, que es el
            //caso de uso que justifica el patron frente al repositorio que guarda por su cuenta
            int rowsAffected;
            await using (var writeContext = CreateContext(dbName))
            {
                var unitOfWork = CreateUnitOfWork(writeContext);
                await unitOfWork._customersUoW.AddAsync(NewCustomer("Uno"));
                await unitOfWork._customersUoW.AddAsync(NewCustomer("Dos"));
                rowsAffected = await unitOfWork.SaveChangesAsync();
            }

            //Assert: contexto nuevo, los dos clientes tienen que estar en el almacen tras una sola llamada
            await using var readContext = CreateContext(dbName);
            var saved = await readContext.Customers.ToListAsync();

            Assert.Equal(2, rowsAffected);
            Assert.Equal(2, saved.Count);
            Assert.Contains(saved, c => c.CompanyName == "Uno");
            Assert.Contains(saved, c => c.CompanyName == "Dos");
        }

        [Fact]
        public async Task AddAsync_RellenaLosCamposDeAuditoria()
        {
            //Arrange
            var dbName = NewDbName();

            //Act: el interceptor se dispara en el SaveChangesAsync del UnitOfWork, no en el repositorio
            await using (var writeContext = CreateContext(dbName))
            {
                var unitOfWork = CreateUnitOfWork(writeContext);
                await unitOfWork._customersUoW.AddAsync(NewCustomer());
                await unitOfWork.SaveChangesAsync();
            }

            //Assert: contexto nuevo, asi que los campos de auditoria se leen del almacen y no de la
            //entidad en memoria que el interceptor acaba de rellenar
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
            await AddCustomerAsync(dbName, customer);

            //Act: contexto nuevo, asi que el FindAsync interno no puede resolverse contra el change
            //tracker y esta obligado a consultar el almacen
            await using var readContext = CreateContext(dbName);
            var unitOfWork = CreateUnitOfWork(readContext);
            var found = await unitOfWork._customersUoW.GetByIdAsync(customer.Id!.Value);

            //Assert: a diferencia de GetAllAsync, GetByIdAsync no usa AsNoTracking, asi que lo que
            //devuelve queda trackeado y es apto para modificarlo y confirmarlo con el UnitOfWork
            Assert.NotNull(found);
            Assert.NotSame(customer, found);
            Assert.Equal("Northwind", found!.CompanyName);
            Assert.Equal(EntityState.Unchanged, readContext.Entry(found).State);
        }

        [Fact]
        public async Task GetByIdAsync_DevuelveNullSiElClienteNoExiste()
        {
            //Arrange: un solo contexto, no hay nada escrito que leer de vuelta
            await using var context = CreateContext(NewDbName());
            var unitOfWork = CreateUnitOfWork(context);

            //Act
            var found = await unitOfWork._customersUoW.GetByIdAsync(999);

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
                var seedUnitOfWork = CreateUnitOfWork(writeContext);
                await seedUnitOfWork._customersUoW.AddAsync(NewCustomer("Uno"));
                await seedUnitOfWork._customersUoW.AddAsync(NewCustomer("Dos"));
                await seedUnitOfWork.SaveChangesAsync();
            }

            //Act: contexto nuevo, asi que la consulta devuelve lo que hay en el almacen y no las
            //instancias que quedaron trackeadas al escribir
            await using var readContext = CreateContext(dbName);
            var unitOfWork = CreateUnitOfWork(readContext);
            var result = await unitOfWork._customersUoW.GetAllAsync();

            //Assert
            Assert.Equal(2, result.Count());
            Assert.Contains(result, c => c.CompanyName == "Uno");
            Assert.Contains(result, c => c.CompanyName == "Dos");
        }

        [Fact]
        public async Task GetAllAsync_DevuelveEntidadesSinTrackear()
        {
            //Arrange
            var dbName = NewDbName();
            var customer = NewCustomer("Original");
            await AddCustomerAsync(dbName, customer);

            //Act: GetAllAsync consulta con AsNoTracking, asi que lo que devuelve queda fuera del change
            //tracker. Modificarlo no genera cambios pendientes y el UnitOfWork no tiene nada que confirmar
            EntityState state;
            int rowsAffected;
            await using (var readContext = CreateContext(dbName))
            {
                var unitOfWork = CreateUnitOfWork(readContext);
                var result = await unitOfWork._customersUoW.GetAllAsync();
                var untracked = Assert.Single(result);
                untracked.CompanyName = "Modificado";
                state = readContext.Entry(untracked).State;
                rowsAffected = await unitOfWork.SaveChangesAsync();
            }

            //Assert: contexto nuevo, el almacen tiene que seguir con el valor original
            await using var verifyContext = CreateContext(dbName);
            var saved = Assert.Single(verifyContext.Customers);

            Assert.Equal(EntityState.Detached, state);
            Assert.Equal(0, rowsAffected);
            Assert.Equal("Original", saved.CompanyName);
        }

        [Fact]
        public async Task GetAllAsync_DevuelveColeccionVaciaSiNoHayClientes()
        {
            //Arrange: un solo contexto, no hay nada escrito que leer de vuelta
            await using var context = CreateContext(NewDbName());
            var unitOfWork = CreateUnitOfWork(context);

            //Act
            var result = await unitOfWork._customersUoW.GetAllAsync();

            //Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task Update_ModificaElClienteTrackeado()
        {
            //Arrange
            var dbName = NewDbName();
            var customer = NewCustomer("Original");
            await AddCustomerAsync(dbName, customer);

            //Act: contexto nuevo, se recarga desde el almacen y se modifica ya trackeado (patron connected).
            //Update() no hace nada en esta rama: quien marca la entidad como Modified es el change tracker
            int rowsAffected;
            EntityState stateBeforeSave;
            await using (var updateContext = CreateContext(dbName))
            {
                var unitOfWork = CreateUnitOfWork(updateContext);
                var tracked = await unitOfWork._customersUoW.GetByIdAsync(customer.Id!.Value);
                tracked!.CompanyName = "Modificado";
                unitOfWork._customersUoW.Update(tracked);
                stateBeforeSave = updateContext.Entry(tracked).State;
                rowsAffected = await unitOfWork.SaveChangesAsync();
            }

            //Assert: contexto nuevo para garantizar que el cambio se guardo de verdad y no se esta
            //leyendo la instancia que se modifico en memoria durante el Act
            await using var readContext = CreateContext(dbName);
            var saved = Assert.Single(readContext.Customers);

            Assert.Equal(EntityState.Modified, stateBeforeSave);
            Assert.Equal(1, rowsAffected);
            Assert.Equal("Modificado", saved.CompanyName);
            Assert.Equal("System", saved.LastUpdatedBy);
        }

        [Fact]
        public async Task Update_ModificaElClienteDetached()
        {
            //Arrange
            var dbName = NewDbName();
            var customer = NewCustomer("Original");
            await AddCustomerAsync(dbName, customer);

            //Act: la entidad se construye fuera y el contexto es nuevo, por lo que entra como Detached
            //y obliga a Update() a tomar la rama del Update() explicito
            var detached = NewCustomer("Detached");
            detached.Id = customer.Id;

            int rowsAffected;
            EntityState stateBeforeSave;
            await using (var updateContext = CreateContext(dbName))
            {
                var unitOfWork = CreateUnitOfWork(updateContext);
                unitOfWork._customersUoW.Update(detached);
                stateBeforeSave = updateContext.Entry(detached).State;
                rowsAffected = await unitOfWork.SaveChangesAsync();
            }

            //Assert: contexto nuevo para garantizar que el cambio se guardo de verdad y no se esta
            //leyendo la instancia detached que se paso al repositorio
            await using var readContext = CreateContext(dbName);
            var saved = Assert.Single(readContext.Customers);

            Assert.Equal(EntityState.Modified, stateBeforeSave);
            Assert.Equal(1, rowsAffected);
            Assert.NotSame(detached, saved);
            Assert.Equal("Detached", saved.CompanyName);
        }

        [Fact]
        public async Task Update_SinSaveChangesNoPersisteElCambio()
        {
            //Arrange
            var dbName = NewDbName();
            var customer = NewCustomer("Original");
            await AddCustomerAsync(dbName, customer);

            //Act: se modifica y se cierra el contexto SIN confirmar
            await using (var updateContext = CreateContext(dbName))
            {
                var unitOfWork = CreateUnitOfWork(updateContext);
                var tracked = await unitOfWork._customersUoW.GetByIdAsync(customer.Id!.Value);
                tracked!.CompanyName = "Modificado";
                unitOfWork._customersUoW.Update(tracked);
            }

            //Assert: contexto nuevo, el almacen tiene que seguir con el valor original
            await using var readContext = CreateContext(dbName);
            var saved = Assert.Single(readContext.Customers);

            Assert.Equal("Original", saved.CompanyName);
        }

        [Fact]
        public async Task Delete_EliminaElClienteExistente()
        {
            //Arrange
            var dbName = NewDbName();
            var customer = NewCustomer();
            await AddCustomerAsync(dbName, customer);

            //Act: contexto nuevo, asi que hay que localizar el cliente en el almacen antes de marcarlo.
            //Delete() recibe la entidad y no el id: la busqueda es responsabilidad del llamante
            int rowsAffected;
            EntityState stateBeforeSave;
            await using (var deleteContext = CreateContext(dbName))
            {
                var unitOfWork = CreateUnitOfWork(deleteContext);
                var tracked = await unitOfWork._customersUoW.GetByIdAsync(customer.Id!.Value);
                unitOfWork._customersUoW.Delete(tracked!);
                stateBeforeSave = deleteContext.Entry(tracked!).State;
                rowsAffected = await unitOfWork.SaveChangesAsync();
            }

            //Assert: contexto nuevo para garantizar que de verdad se ejecuto todo el proceso y que el
            //resultado no sale de una instancia trackeada anteriormente
            await using var readContext = CreateContext(dbName);

            Assert.Equal(EntityState.Deleted, stateBeforeSave);
            Assert.Equal(1, rowsAffected);
            Assert.Empty(readContext.Customers);
        }

        [Fact]
        public async Task Delete_SinSaveChangesNoEliminaNada()
        {
            //Arrange
            var dbName = NewDbName();
            var customer = NewCustomer("Contoso");
            await AddCustomerAsync(dbName, customer);

            //Act: se marca para borrado y se cierra el contexto SIN confirmar
            await using (var deleteContext = CreateContext(dbName))
            {
                var unitOfWork = CreateUnitOfWork(deleteContext);
                var tracked = await unitOfWork._customersUoW.GetByIdAsync(customer.Id!.Value);
                unitOfWork._customersUoW.Delete(tracked!);
            }

            //Assert: contexto nuevo, el cliente tiene que seguir en el almacen
            await using var readContext = CreateContext(dbName);
            var saved = Assert.Single(readContext.Customers);

            Assert.Equal("Contoso", saved.CompanyName);
        }

        [Fact]
        public async Task Delete_EliminaSoloElClienteIndicado()
        {
            //Arrange
            var dbName = NewDbName();
            var superviviente = NewCustomer("Superviviente");
            var borrado = NewCustomer("Borrado");
            await using (var seedContext = CreateContext(dbName))
            {
                var seedUnitOfWork = CreateUnitOfWork(seedContext);
                await seedUnitOfWork._customersUoW.AddAsync(superviviente);
                await seedUnitOfWork._customersUoW.AddAsync(borrado);
                await seedUnitOfWork.SaveChangesAsync();
            }

            //Act: contexto nuevo, se borra solo uno de los dos
            int rowsAffected;
            await using (var deleteContext = CreateContext(dbName))
            {
                var unitOfWork = CreateUnitOfWork(deleteContext);
                var tracked = await unitOfWork._customersUoW.GetByIdAsync(borrado.Id!.Value);
                unitOfWork._customersUoW.Delete(tracked!);
                rowsAffected = await unitOfWork.SaveChangesAsync();
            }

            //Assert: contexto nuevo, el borrado no puede haber arrastrado al resto del almacen
            await using var readContext = CreateContext(dbName);
            var saved = Assert.Single(readContext.Customers);

            Assert.Equal(1, rowsAffected);
            Assert.Equal("Superviviente", saved.CompanyName);
        }
    }
}