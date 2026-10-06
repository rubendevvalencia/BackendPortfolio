using Ecommerce.Application.Feature.ProductToCustomer;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interface.IRepository;
using Ecommerce.Domain.Interface.IRepository.IProduct;
using Ecommerce.Transversal.Common.Enums;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Ecommerce.Test.ApplicationTest.Feature.ProductToCustomer
{
    //Tests del handler de SaveProduct con dobles; sin caso de validacion porque valida ValidationBehaviour.
    public class ProductToCustomerTests : ApplicationTestBase
    {
        //Dependencias falsas: xUnit crea una instancia de la clase por test, asi que cada test parte de dobles nuevos.
        private readonly ICustomerRepositoryUoW customerRepository = Substitute.For<ICustomerRepositoryUoW>();
        private readonly IProductRepository productRepository = Substitute.For<IProductRepository>();
        private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

        [Fact]
        public async Task Handle_SaveProduct_Ok()
        {
            //Arrange: cliente sin productos (Products es null) y producto existentes.
            unitOfWork._customersUoW.Returns(customerRepository);
            unitOfWork._products.Returns(productRepository);
            var handler = new ProductToCustomerCommandHandle(unitOfWork);

            var customer = NewCustomer(5);
            var product = new Product 
            { 
                Id = 7, 
                Name = "Test" 
            };
            
            customerRepository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(customer);
            productRepository.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(product);

            //El commit escribe una fila.
            unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

            var request = new ProductToCustomerCommand
            {
                CustomerId = 5,
                ProductId = 7
            };

            //Act
            var response = await handler.Handle(request, CancellationToken.None);

            //Assert: la respuesta es correcta.
            Assert.True(response.IsSuccess);
            Assert.True(response.Data);

            //Assert: el producto queda en la coleccion del cliente, que se crea al estar a null.
            Assert.NotNull(customer.Products);
            var numeroDeProductos = customer.Products.Count;
            Assert.Equal(1, numeroDeProductos);
            Assert.Contains(product, customer.Products);

            //Assert: se actualiza y se confirma una vez.
            customerRepository.Received(1).Update(customer);
            await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_SaveProduct_ClienteConProductos_ConservaLosAnteriores()
        {
            //Arrange: el cliente ya tiene un producto asociado.
            unitOfWork._customersUoW.Returns(customerRepository);
            unitOfWork._products.Returns(productRepository);
            var handler = new ProductToCustomerCommandHandle(unitOfWork);

            var anterior = new Product
            {
                Id = 1,
                Name = "Anterior"
            };
            var customer = NewCustomer(5);
            customer.Products = new List<Product> { anterior };
            var nuevo = new Product
            {
                Id = 7,
                Name = "Nuevo"
            };
            customerRepository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(customer);
            productRepository.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(nuevo);
            unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

            var request = new ProductToCustomerCommand
            {
                CustomerId = 5,
                ProductId = 7
            };

            //Act
            var response = await handler.Handle(request, CancellationToken.None);

            //Assert: se anade el nuevo sin perder el anterior.
            Assert.True(response.IsSuccess);
            Assert.Equal(2, customer.Products.Count);
            Assert.Contains(anterior, customer.Products);
            Assert.Contains(nuevo, customer.Products);
        }

        [Fact]
        public async Task Handle_SaveProduct_CustomerNotFound()
        {
            //Arrange: el cliente no existe, el producto si.
            unitOfWork._customersUoW.Returns(customerRepository);
            unitOfWork._products.Returns(productRepository);
            var handler = new ProductToCustomerCommandHandle(unitOfWork);

            var product = new Product
            {
                Id = 7
            };
            customerRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Customer?)null);
            productRepository.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(product);

            var request = new ProductToCustomerCommand
            {
                CustomerId = 99,
                ProductId = 7
            };

            //Act
            var response = await handler.Handle(request, CancellationToken.None);

            //Assert: la respuesta es NotFound.
            Assert.False(response.IsSuccess);
            Assert.Equal(ErrorType.NotFound, response.ErrorType);
            Assert.Equal("Product or Customer not found", response.Message);

            //Assert: no se actualiza ni se confirma.
            customerRepository.DidNotReceive().Update(Arg.Any<Customer>());
            await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_SaveProduct_ProductNotFound()
        {
            //Arrange: el cliente existe, el producto no.
            unitOfWork._customersUoW.Returns(customerRepository);
            unitOfWork._products.Returns(productRepository);
            var handler = new ProductToCustomerCommandHandle(unitOfWork);

            var customer = NewCustomer(5);
            customerRepository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(customer);
            productRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Product?)null);

            var request = new ProductToCustomerCommand
            {
                CustomerId = 5,
                ProductId = 99
            };

            //Act
            var response = await handler.Handle(request, CancellationToken.None);

            //Assert: la respuesta es NotFound y el cliente no se toca.
            Assert.False(response.IsSuccess);
            Assert.Equal(ErrorType.NotFound, response.ErrorType);
            Assert.Equal("Product or Customer not found", response.Message);
            Assert.Null(customer.Products);

            //Assert: no se actualiza ni se confirma.
            customerRepository.DidNotReceive().Update(Arg.Any<Customer>());
            await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_SaveProduct_BothNotFound()
        {
            //Arrange: no existe ni el cliente ni el producto.
            unitOfWork._customersUoW.Returns(customerRepository);
            unitOfWork._products.Returns(productRepository);
            var handler = new ProductToCustomerCommandHandle(unitOfWork);

            customerRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Customer?)null);
            productRepository.GetByIdAsync(98, Arg.Any<CancellationToken>()).Returns((Product?)null);

            var request = new ProductToCustomerCommand
            {
                CustomerId = 99,
                ProductId = 98
            };

            //Act
            var response = await handler.Handle(request, CancellationToken.None);

            //Assert: la respuesta es NotFound.
            Assert.False(response.IsSuccess);
            Assert.Equal(ErrorType.NotFound, response.ErrorType);
            await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_SaveProduct_SaveFail()
        {
            //Arrange: ambos existen pero el commit no escribe ninguna fila.
            unitOfWork._customersUoW.Returns(customerRepository);
            unitOfWork._products.Returns(productRepository);
            var handler = new ProductToCustomerCommandHandle(unitOfWork);

            var customer = NewCustomer(5);
            var product = new Product
            {
                Id = 7
            };
            customerRepository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(customer);
            productRepository.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(product);
            unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(0);

            var request = new ProductToCustomerCommand
            {
                CustomerId = 5,
                ProductId = 7
            };

            //Act
            var response = await handler.Handle(request, CancellationToken.None);

            //Assert: la respuesta es un fallo de operacion invalida.
            Assert.False(response.IsSuccess);
            Assert.Equal(ErrorType.InvalidOperation, response.ErrorType);
            Assert.Equal("Product could not be added.", response.Message);
        }

        [Fact]
        public async Task Handle_SaveProduct_Exception()
        {
            //Arrange: falla la base de datos.
            unitOfWork._customersUoW.Returns(customerRepository);
            unitOfWork._products.Returns(productRepository);
            var handler = new ProductToCustomerCommandHandle(unitOfWork);

            var customer = NewCustomer(5);
            var product = new Product
            {
                Id = 7
            };
            customerRepository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(customer);
            productRepository.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(product);
            unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException("problem in db"));

            var request = new ProductToCustomerCommand
            {
                CustomerId = 5,
                ProductId = 7
            };

            //Act
            Func<Task> work = () => handler.Handle(request, CancellationToken.None);

            //Assert: la excepcion no se captura.
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(work);
            Assert.Equal("problem in db", exception.Message);
        }

        [Fact]
        public async Task Handle_SaveProduct_BuscaCadaEntidadPorSuPropioId()
        {
            //Arrange: Ids distintos para detectar que no se intercambian el del cliente y el del producto.
            unitOfWork._customersUoW.Returns(customerRepository);
            unitOfWork._products.Returns(productRepository);
            var handler = new ProductToCustomerCommandHandle(unitOfWork);

            var customer = NewCustomer(5);
            var product = new Product
            {
                Id = 7
            };
            customerRepository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(customer);
            productRepository.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(product);
            unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

            var request = new ProductToCustomerCommand
            {
                CustomerId = 5,
                ProductId = 7
            };

            //Act
            await handler.Handle(request, CancellationToken.None);

            //Assert: cada repositorio se consulta una vez y con su Id.
            await customerRepository.Received(1).GetByIdAsync(5, Arg.Any<CancellationToken>());
            await productRepository.Received(1).GetByIdAsync(7, Arg.Any<CancellationToken>());
            await customerRepository.DidNotReceive().GetByIdAsync(7, Arg.Any<CancellationToken>());
            await productRepository.DidNotReceive().GetByIdAsync(5, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_SaveProduct_PropagaElCancellationTokenAlCommit()
        {
            //Arrange: un token propio para comprobar que llega al SaveChangesAsync.
            unitOfWork._customersUoW.Returns(customerRepository);
            unitOfWork._products.Returns(productRepository);
            var handler = new ProductToCustomerCommandHandle(unitOfWork);

            var customer = NewCustomer(5);
            var product = new Product
            {
                Id = 7
            };
            customerRepository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(customer);
            productRepository.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(product);
            unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

            var request = new ProductToCustomerCommand
            {
                CustomerId = 5,
                ProductId = 7
            };
            using var tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token;

            //Act
            await handler.Handle(request, token);

            //Assert: se confirma una vez y con el mismo token que recibio el handler.
            await unitOfWork.Received(1).SaveChangesAsync(token);
        }

        [Fact]
        public async Task Handle_SaveProduct_CancelacionDuranteElCommit()
        {
            //Arrange: la peticion se cancela mientras se guarda.
            unitOfWork._customersUoW.Returns(customerRepository);
            unitOfWork._products.Returns(productRepository);
            var handler = new ProductToCustomerCommandHandle(unitOfWork);

            var customer = NewCustomer(5);
            var product = new Product
            {
                Id = 7
            };
            customerRepository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(customer);
            productRepository.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(product);
            unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new OperationCanceledException());

            var request = new ProductToCustomerCommand
            {
                CustomerId = 5,
                ProductId = 7
            };

            //Act
            Func<Task> work = () => handler.Handle(request, CancellationToken.None);

            //Assert: la cancelacion no se captura.
            await Assert.ThrowsAsync<OperationCanceledException>(work);
        }

        [Fact]
        public async Task Handle_SaveProduct_FallaLaBusquedaDelProducto_NoGuarda()
        {
            //Arrange: el cliente se encuentra pero la consulta del producto lanza una excepcion.
            unitOfWork._customersUoW.Returns(customerRepository);
            unitOfWork._products.Returns(productRepository);
            var handler = new ProductToCustomerCommandHandle(unitOfWork);

            var customer = NewCustomer(5);
            customerRepository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(customer);
            productRepository.GetByIdAsync(7, Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException("problem in db"));

            var request = new ProductToCustomerCommand
            {
                CustomerId = 5,
                ProductId = 7
            };

            //Act
            Func<Task> work = () => handler.Handle(request, CancellationToken.None);

            //Assert: la excepcion no se captura y el cliente queda intacto.
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(work);
            Assert.Equal("problem in db", exception.Message);
            Assert.Null(customer.Products);

            //Assert: no se actualiza ni se confirma.
            customerRepository.DidNotReceive().Update(Arg.Any<Customer>());
            await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        //Cada fila: el commit escribe una o varias filas (la tabla de union puede tocar mas de una) y siempre es exito.
        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(10)]
        public async Task Handle_SaveProduct_CualquierNumeroDeFilasPositivoEsExito(int filasEscritas)
        {
            //Arrange
            unitOfWork._customersUoW.Returns(customerRepository);
            unitOfWork._products.Returns(productRepository);
            var handler = new ProductToCustomerCommandHandle(unitOfWork);

            var customer = NewCustomer(5);
            var product = new Product
            {
                Id = 7
            };
            customerRepository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(customer);
            productRepository.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(product);
            unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(filasEscritas);

            var request = new ProductToCustomerCommand
            {
                CustomerId = 5,
                ProductId = 7
            };

            //Act
            var response = await handler.Handle(request, CancellationToken.None);

            //Assert
            Assert.True(response.IsSuccess);
            Assert.True(response.Data);
            Assert.Equal(ErrorType.None, response.ErrorType);
        }

        //Comportamiento actual, no el deseado: el handler no comprueba si el producto ya esta asociado y lo duplica.
        [Fact]
        public async Task Handle_SaveProduct_ProductoYaAsociado_LoDuplica()
        {
            //Arrange: el cliente ya tiene el mismo producto.
            unitOfWork._customersUoW.Returns(customerRepository);
            unitOfWork._products.Returns(productRepository);
            var handler = new ProductToCustomerCommandHandle(unitOfWork);

            var product = new Product
            {
                Id = 7
            };
            var customer = NewCustomer(5);
            customer.Products = new List<Product> { product };
            customerRepository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(customer);
            productRepository.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(product);
            unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

            var request = new ProductToCustomerCommand
            {
                CustomerId = 5,
                ProductId = 7
            };

            //Act
            var response = await handler.Handle(request, CancellationToken.None);

            //Assert: la respuesta es correcta y el producto aparece dos veces.
            Assert.True(response.IsSuccess);
            var numeroDeProductos = customer.Products.Count;
            Assert.Equal(2, numeroDeProductos);
        }
    }
}
