using Ecommerce.Application.Feature.Products.Commands.CreateProduct;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Enum;
using Ecommerce.Domain.Interface.IRepository;
using Ecommerce.Domain.Interface.IRepository.IProduct;
using Ecommerce.Transversal.Common.Enums;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Ecommerce.Test.ApplicationTest.Feature.Products.Commands.CreateProduct
{
    //Tests del handler de CreateProduct con dobles; sin caso de validacion porque valida ValidationBehaviour.
    public class CreateProductTests : ApplicationTestBase
    {
        //Dependencias falsas: xUnit crea una instancia de la clase por test, asi que cada test parte de dobles nuevos.
        private readonly IProductRepository productRepository = Substitute.For<IProductRepository>();
        private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

        [Fact]
        public async Task Handle_CreateProduct_Ok()
        {
            //Arrange
            unitOfWork._products.Returns(productRepository);
            var handler = new CreateProductCommandHandle(unitOfWork, Mapper);

            var request = new CreateProductCommand
            {
                Name = "Libro",
                Description = "Novela",
                Price = 10.5m,
                StockQuantity = 5,
                Category = 1
            };

            //El producto todavia no existe en la base de datos.
            productRepository.CompareInfoInDb(Arg.Any<Product>(), Arg.Any<CancellationToken>()).Returns(false);

            //Guardamos el Product que recibe AddAsync.
            Product? productGuardado = null;
            productRepository
                .When(repo => repo.AddAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>()))
                .Do(llamada => productGuardado = llamada.Arg<Product>());

            //El commit escribe una fila.
            unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

            //Act
            var response = await handler.Handle(request, CancellationToken.None);

            //Assert: la respuesta es correcta.
            Assert.True(response.IsSuccess);
            Assert.True(response.Data);

            //Assert: el comando se mapea al producto y el Id va a null porque lo genera la base de datos.
            Assert.NotNull(productGuardado);
            Assert.Null(productGuardado.Id);
            Assert.Equal("Libro", productGuardado.Name);
            Assert.Equal("Novela", productGuardado.Description);
            Assert.Equal(10.5m, productGuardado.Price);
            Assert.Equal(5, productGuardado.StockQuantity);
            Assert.Equal(Category.Book, productGuardado.Category);

            //Assert: se confirmo el cambio una sola vez.
            await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_CreateProduct_Duplicated()
        {
            //Arrange
            unitOfWork._products.Returns(productRepository);
            var handler = new CreateProductCommandHandle(unitOfWork, Mapper);

            var request = new CreateProductCommand
            {
                Name = "Libro",
                Description = "Novela",
                Price = 10.5m,
                StockQuantity = 5,
                Category = 1
            };

            //El producto YA existe en la base de datos.
            productRepository.CompareInfoInDb(Arg.Any<Product>(), Arg.Any<CancellationToken>()).Returns(true);

            //Act
            var response = await handler.Handle(request, CancellationToken.None);

            //Assert: la respuesta es un fallo por duplicado.
            Assert.False(response.IsSuccess);
            Assert.Equal(ErrorType.Duplicated, response.ErrorType);
            Assert.Equal("Product is already registered", response.Message);

            //Assert: si ya existe, no se añade ni se confirma nada.
            await productRepository.DidNotReceive().AddAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
            await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_CreateProduct_SaveFail()
        {
            //Arrange
            unitOfWork._products.Returns(productRepository);
            var handler = new CreateProductCommandHandle(unitOfWork, Mapper);

            var request = new CreateProductCommand
            {
                Name = "Libro",
                Description = "Novela",
                Price = 10.5m,
                StockQuantity = 5,
                Category = 1
            };

            //El producto todavia no existe en la base de datos.
            productRepository.CompareInfoInDb(Arg.Any<Product>(), Arg.Any<CancellationToken>()).Returns(false);

            //El commit no escribe ninguna fila.
            unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(0);

            //Act
            var response = await handler.Handle(request, CancellationToken.None);

            //Assert: la respuesta es un fallo inesperado.
            Assert.False(response.IsSuccess);
            Assert.False(response.Data);
            Assert.Equal(ErrorType.Unexpected, response.ErrorType);
            Assert.Equal("Product could not be added.", response.Message);

            //Assert: el handler si intento añadir y confirmar, pero el commit no escribio nada.
            await productRepository.Received(1).AddAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
            await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_CreateProduct_Exception()
        {
            //Arrange
            unitOfWork._products.Returns(productRepository);
            var handler = new CreateProductCommandHandle(unitOfWork, Mapper);

            var request = new CreateProductCommand
            {
                Name = "Libro",
                Description = "Novela",
                Price = 10.5m,
                StockQuantity = 5,
                Category = 1
            };

            //El producto todavia no existe, pero falla la base de datos.
            productRepository.CompareInfoInDb(Arg.Any<Product>(), Arg.Any<CancellationToken>()).Returns(false);
            unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException("problem in db"));

            //Act
            Func<Task> work = () => handler.Handle(request, CancellationToken.None);

            //Assert: la excepcion no se captura.
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(work);
            Assert.Equal("problem in db", exception.Message);
        }

        [Fact]
        public async Task Handle_CreateProduct_FallaLaComprobacionDeDuplicado_NoGuarda()
        {
            //Arrange: la consulta de duplicados lanza una excepcion.
            unitOfWork._products.Returns(productRepository);
            var handler = new CreateProductCommandHandle(unitOfWork, Mapper);

            var request = new CreateProductCommand
            {
                Name = "Libro",
                Description = "Novela",
                Price = 10.5m,
                StockQuantity = 5,
                Category = 1
            };
            productRepository.CompareInfoInDb(Arg.Any<Product>(), Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException("problem in db"));

            //Act
            Func<Task> work = () => handler.Handle(request, CancellationToken.None);

            //Assert: la excepcion no se captura.
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(work);
            Assert.Equal("problem in db", exception.Message);

            //Assert: no se añade ni se confirma nada.
            await productRepository.DidNotReceive().AddAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
            await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_CreateProduct_PropagaElCancellationTokenATodosLosPasos()
        {
            //Arrange: un token propio para comprobar que llega a la comprobacion, al AddAsync y al commit.
            unitOfWork._products.Returns(productRepository);
            var handler = new CreateProductCommandHandle(unitOfWork, Mapper);

            var request = new CreateProductCommand
            {
                Name = "Libro",
                Description = "Novela",
                Price = 10.5m,
                StockQuantity = 5,
                Category = 1
            };
            productRepository.CompareInfoInDb(Arg.Any<Product>(), Arg.Any<CancellationToken>()).Returns(false);
            unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
            using var tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token;

            //Act
            await handler.Handle(request, token);

            //Assert: cada paso se hace una vez y con el mismo token que recibio el handler.
            await productRepository.Received(1).CompareInfoInDb(Arg.Any<Product>(), token);
            await productRepository.Received(1).AddAsync(Arg.Any<Product>(), token);
            await unitOfWork.Received(1).SaveChangesAsync(token);
        }

        //Cada fila: el commit escribe una o varias filas y siempre es exito.
        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(10)]
        public async Task Handle_CreateProduct_CualquierNumeroDeFilasPositivoEsExito(int filasEscritas)
        {
            //Arrange
            unitOfWork._products.Returns(productRepository);
            var handler = new CreateProductCommandHandle(unitOfWork, Mapper);

            var request = new CreateProductCommand
            {
                Name = "Libro",
                Description = "Novela",
                Price = 10.5m,
                StockQuantity = 5,
                Category = 1
            };
            productRepository.CompareInfoInDb(Arg.Any<Product>(), Arg.Any<CancellationToken>()).Returns(false);
            unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(filasEscritas);

            //Act
            var response = await handler.Handle(request, CancellationToken.None);

            //Assert
            Assert.True(response.IsSuccess);
            Assert.True(response.Data);
            Assert.Equal(ErrorType.None, response.ErrorType);
        }

        //Cada fila: el valor numerico de Category y la categoria a la que tiene que mapearse.
        [Theory]
        [InlineData(1, Category.Book)]
        [InlineData(2, Category.Electronics)]
        [InlineData(3, Category.Clothing)]
        [InlineData(4, Category.Home)]
        [InlineData(5, Category.Other)]
        public async Task Handle_CreateProduct_MapeaLaCategoria(int categoria, Category categoriaEsperada)
        {
            //Arrange
            unitOfWork._products.Returns(productRepository);
            var handler = new CreateProductCommandHandle(unitOfWork, Mapper);

            var request = new CreateProductCommand
            {
                Name = "Libro",
                Description = "Novela",
                Price = 10.5m,
                StockQuantity = 5,
                Category = categoria
            };
            productRepository.CompareInfoInDb(Arg.Any<Product>(), Arg.Any<CancellationToken>()).Returns(false);
            Product? productGuardado = null;
            productRepository
                .When(repo => repo.AddAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>()))
                .Do(llamada => productGuardado = llamada.Arg<Product>());
            unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

            //Act
            await handler.Handle(request, CancellationToken.None);

            //Assert
            Assert.NotNull(productGuardado);
            Assert.Equal(categoriaEsperada, productGuardado.Category);
        }
    }
}
