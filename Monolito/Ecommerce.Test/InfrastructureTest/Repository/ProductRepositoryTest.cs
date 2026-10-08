using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Enum;
using Ecommerce.Infrastructure.Data;
using Ecommerce.Infrastructure.Interceptors;
using Ecommerce.Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Ecommerce.Test.InfrastructureTest.Repository
{
    public class ProductRepositoryTest
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

        //Guarda un producto con su propio contexto. El repositorio no confirma: lo hace el SaveChangesAsync del contexto.
        private static async Task AddProductAsync(string dbName, Product product)
        {
            await using var context = CreateContext(dbName);
            var repository = new ProductRepository(context);
            await repository.AddAsync(product);
            await context.SaveChangesAsync();
        }

        [Fact]
        public async Task AddAsync_PersisteElProductoEnElAlmacen()
        {
            //Arrange
            var dbName = NewDbName();
            var product = NewProduct("Libro");

            //Act: el repositorio solo marca la entidad, la confirmacion la hace el contexto
            int rowsAffected;
            await using (var writeContext = CreateContext(dbName))
            {
                var repository = new ProductRepository(writeContext);
                await repository.AddAsync(product);
                rowsAffected = await writeContext.SaveChangesAsync();
            }

            //Assert: contexto nuevo, asi que el producto viene del almacen y no es la misma instancia.
            await using var readContext = CreateContext(dbName);
            var products = readContext.Products.ToList();
            var numeroDeProductos = products.Count;
            Assert.Equal(1, numeroDeProductos);
            var saved = products[0];

            Assert.Equal(1, rowsAffected);
            Assert.NotSame(product, saved);
            Assert.Equal("Libro", saved.Name);
            Assert.Equal(Category.Book, saved.Category);
        }

        [Fact]
        public async Task AddAsync_SinSaveChangesNoPersisteNada()
        {
            //Arrange
            var dbName = NewDbName();
            var product = NewProduct();

            //Act: se anade y se cierra el contexto sin confirmar, asi que no se guarda nada.
            EntityState stateAfterAdd;
            await using (var writeContext = CreateContext(dbName))
            {
                var repository = new ProductRepository(writeContext);
                await repository.AddAsync(product);
                stateAfterAdd = writeContext.Entry(product).State;
            }

            //Assert: contexto nuevo, asi que el almacen es la unica fuente de verdad
            await using var readContext = CreateContext(dbName);

            Assert.Equal(EntityState.Added, stateAfterAdd);
            Assert.Empty(readContext.Products);
        }

        [Fact]
        public async Task GetByIdAsync_DevuelveElProductoGuardado()
        {
            //Arrange
            var dbName = NewDbName();
            var product = NewProduct("Portatil");
            await AddProductAsync(dbName, product);

            //Act: contexto nuevo, asi que FindAsync tiene que ir al almacen.
            await using var readContext = CreateContext(dbName);
            var repository = new ProductRepository(readContext);
            var found = await repository.GetByIdAsync(product.Id!.Value);

            //Assert: GetByIdAsync no usa AsNoTracking, asi que la entidad queda trackeada.
            Assert.NotNull(found);
            Assert.NotSame(product, found);
            Assert.Equal("Portatil", found!.Name);
            Assert.Equal(EntityState.Unchanged, readContext.Entry(found).State);
        }

        [Fact]
        public async Task GetByIdAsync_DevuelveNullSiElProductoNoExiste()
        {
            //Arrange: un solo contexto, no hay nada escrito que leer de vuelta
            await using var context = CreateContext(NewDbName());
            var repository = new ProductRepository(context);

            //Act
            var found = await repository.GetByIdAsync(999);

            //Assert
            Assert.Null(found);
        }

        [Fact]
        public async Task GetAllAsync_DevuelveTodosLosProductos()
        {
            //Arrange
            var dbName = NewDbName();
            await using (var writeContext = CreateContext(dbName))
            {
                var seedRepository = new ProductRepository(writeContext);
                await seedRepository.AddAsync(NewProduct("Uno"));
                await seedRepository.AddAsync(NewProduct("Dos"));
                await writeContext.SaveChangesAsync();
            }

            //Act: contexto nuevo, asi que la consulta lee del almacen.
            await using var readContext = CreateContext(dbName);
            var repository = new ProductRepository(readContext);
            var result = await repository.GetAllAsync();

            //Assert
            var products = result.ToList();
            var numeroDeProductos = products.Count;
            Assert.Equal(2, numeroDeProductos);
            Assert.Equal("Uno", products[0].Name);
            Assert.Equal("Dos", products[1].Name);
        }

        [Fact]
        public async Task GetAllAsync_DevuelveEntidadesSinTrackear()
        {
            //Arrange
            var dbName = NewDbName();
            var product = NewProduct("Original");
            await AddProductAsync(dbName, product);

            //Act: GetAllAsync usa AsNoTracking, asi que modificar lo devuelto no deja cambios pendientes.
            EntityState state;
            int rowsAffected;
            await using (var readContext = CreateContext(dbName))
            {
                var repository = new ProductRepository(readContext);
                var result = await repository.GetAllAsync();
                var untracked = result.First();
                untracked.Name = "Modificado";
                state = readContext.Entry(untracked).State;
                rowsAffected = await readContext.SaveChangesAsync();
            }

            //Assert: contexto nuevo, el almacen tiene que seguir con el valor original
            await using var verifyContext = CreateContext(dbName);
            var products = verifyContext.Products.ToList();
            var numeroDeProductos = products.Count;
            Assert.Equal(1, numeroDeProductos);
            var saved = products[0];

            Assert.Equal(EntityState.Detached, state);
            Assert.Equal(0, rowsAffected);
            Assert.Equal("Original", saved.Name);
        }

        [Fact]
        public async Task GetAllAsync_DevuelveColeccionVaciaSiNoHayProductos()
        {
            //Arrange: un solo contexto, no hay nada escrito que leer de vuelta
            await using var context = CreateContext(NewDbName());
            var repository = new ProductRepository(context);

            //Act
            var result = await repository.GetAllAsync();

            //Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task Update_ModificaElProductoTrackeado()
        {
            //Arrange
            var dbName = NewDbName();
            var product = NewProduct("Original");
            await AddProductAsync(dbName, product);

            //Act: contexto nuevo, se recarga y se modifica ya trackeado.
            int rowsAffected;
            EntityState stateBeforeSave;
            await using (var updateContext = CreateContext(dbName))
            {
                var repository = new ProductRepository(updateContext);
                var tracked = await repository.GetByIdAsync(product.Id!.Value);
                tracked!.Name = "Modificado";
                repository.Update(tracked);
                stateBeforeSave = updateContext.Entry(tracked).State;
                rowsAffected = await updateContext.SaveChangesAsync();
            }

            //Assert: contexto nuevo para comprobar que el cambio se guardo de verdad.
            await using var readContext = CreateContext(dbName);
            var products = readContext.Products.ToList();
            var numeroDeProductos = products.Count;
            Assert.Equal(1, numeroDeProductos);
            var saved = products[0];

            Assert.Equal(EntityState.Modified, stateBeforeSave);
            Assert.Equal(1, rowsAffected);
            Assert.Equal("Modificado", saved.Name);
        }

        [Fact]
        public async Task Update_ModificaElProductoDetached()
        {
            //Arrange
            var dbName = NewDbName();
            var product = NewProduct("Original");
            await AddProductAsync(dbName, product);

            //Act: entidad creada fuera y contexto nuevo, asi que entra como Detached.
            var detached = NewProduct("Detached");
            detached.Id = product.Id;

            int rowsAffected;
            EntityState stateBeforeSave;
            await using (var updateContext = CreateContext(dbName))
            {
                var repository = new ProductRepository(updateContext);
                repository.Update(detached);
                stateBeforeSave = updateContext.Entry(detached).State;
                rowsAffected = await updateContext.SaveChangesAsync();
            }

            //Assert: contexto nuevo para comprobar que el cambio se guardo de verdad.
            await using var readContext = CreateContext(dbName);
            var products = readContext.Products.ToList();
            var numeroDeProductos = products.Count;
            Assert.Equal(1, numeroDeProductos);
            var saved = products[0];

            Assert.Equal(EntityState.Modified, stateBeforeSave);
            Assert.Equal(1, rowsAffected);
            Assert.NotSame(detached, saved);
            Assert.Equal("Detached", saved.Name);
        }

        [Fact]
        public async Task Delete_EliminaElProductoExistente()
        {
            //Arrange
            var dbName = NewDbName();
            var product = NewProduct();
            await AddProductAsync(dbName, product);

            //Act: contexto nuevo, se busca el producto antes de borrarlo.
            int rowsAffected;
            EntityState stateBeforeSave;
            await using (var deleteContext = CreateContext(dbName))
            {
                var repository = new ProductRepository(deleteContext);
                var tracked = await repository.GetByIdAsync(product.Id!.Value);
                repository.Delete(tracked!);
                stateBeforeSave = deleteContext.Entry(tracked!).State;
                rowsAffected = await deleteContext.SaveChangesAsync();
            }

            //Assert: contexto nuevo para comprobar el resultado en el almacen.
            await using var readContext = CreateContext(dbName);

            Assert.Equal(EntityState.Deleted, stateBeforeSave);
            Assert.Equal(1, rowsAffected);
            Assert.Empty(readContext.Products);
        }

        [Fact]
        public async Task Delete_SinSaveChangesNoEliminaNada()
        {
            //Arrange
            var dbName = NewDbName();
            var product = NewProduct("Libro");
            await AddProductAsync(dbName, product);

            //Act: se marca para borrado y se cierra el contexto SIN confirmar
            await using (var deleteContext = CreateContext(dbName))
            {
                var repository = new ProductRepository(deleteContext);
                var tracked = await repository.GetByIdAsync(product.Id!.Value);
                repository.Delete(tracked!);
            }

            //Assert: contexto nuevo, el producto tiene que seguir en el almacen
            await using var readContext = CreateContext(dbName);
            var products = readContext.Products.ToList();
            var numeroDeProductos = products.Count;
            Assert.Equal(1, numeroDeProductos);
            var saved = products[0];

            Assert.Equal("Libro", saved.Name);
        }

        [Fact]
        public async Task CompareInfoInDb_DevuelveTrueSiHayUnProductoIgual()
        {
            //Arrange
            var dbName = NewDbName();
            await AddProductAsync(dbName, NewProduct("Libro"));

            //Act: contexto nuevo y un producto distinto instancia pero con los mismos datos.
            await using var readContext = CreateContext(dbName);
            var repository = new ProductRepository(readContext);
            var exists = await repository.CompareInfoInDb(NewProduct("Libro"));

            //Assert
            Assert.True(exists);
        }

        [Fact]
        public async Task CompareInfoInDb_DevuelveFalseSiAlgunDatoDifiere()
        {
            //Arrange
            var dbName = NewDbName();
            await AddProductAsync(dbName, NewProduct("Libro"));

            //Act: mismo producto pero con otro precio.
            var different = NewProduct("Libro");
            different.Price = 99;

            await using var readContext = CreateContext(dbName);
            var repository = new ProductRepository(readContext);
            var exists = await repository.CompareInfoInDb(different);

            //Assert
            Assert.False(exists);
        }
    }
}
