using Ecommerce.Application.Feature.ProductToCustomer;

namespace Ecommerce.Test.ApplicationTest.Feature.ProductToCustomer
{
    //Tests del validador de SaveProduct por si solo, sin handler ni dobles: se le pasa un comando y se mira el resultado.
    public class ProductToCustomerValidatorTests
    {
        private readonly ProductToCustomerValidator _validator = new ProductToCustomerValidator();

        [Fact]
        public void Validate_CommandValido_NoTieneErrores()
        {
            //Arrange
            var request = new ProductToCustomerCommand
            {
                ProductId = 1,
                CustomerId = 1
            };

            //Act
            var resultado = _validator.Validate(request);

            //Assert
            Assert.True(resultado.IsValid);
            var numeroDeErrores = resultado.Errors.Count;
            Assert.Equal(0, numeroDeErrores);
        }

        //Con 0 saltan las dos reglas (NotEmpty y GreaterThan), porque 0 es el valor por defecto del int.
        [Fact]
        public void Validate_ProductIdCero_DevuelveDosErrores()
        {
            //Arrange
            var request = new ProductToCustomerCommand
            {
                ProductId = 0,
                CustomerId = 1
            };

            //Act
            var resultado = _validator.Validate(request);

            //Assert: no es valido y los dos errores son del ProductId.
            Assert.False(resultado.IsValid);
            var numeroDeErrores = resultado.Errors.Count;
            Assert.Equal(2, numeroDeErrores);

            var primerError = resultado.Errors[0];
            Assert.Equal(nameof(ProductToCustomerCommand.ProductId), primerError.PropertyName);
            Assert.Equal("ProductId is required.", primerError.ErrorMessage);

            var segundoError = resultado.Errors[1];
            Assert.Equal(nameof(ProductToCustomerCommand.ProductId), segundoError.PropertyName);
            Assert.Equal("ProductId must be greater than 0.", segundoError.ErrorMessage);
        }

        //Cada fila: un ProductId negativo, incluido el limite inferior del int.
        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        [InlineData(int.MinValue)]
        public void Validate_ProductIdNegativo_DevuelveError(int productId)
        {
            //Arrange
            var request = new ProductToCustomerCommand
            {
                ProductId = productId,
                CustomerId = 1
            };

            //Act
            var resultado = _validator.Validate(request);

            //Assert: no es valido y hay un unico error, el del ProductId.
            Assert.False(resultado.IsValid);
            var numeroDeErrores = resultado.Errors.Count;
            Assert.Equal(1, numeroDeErrores);

            var error = resultado.Errors[0];
            Assert.Equal(nameof(ProductToCustomerCommand.ProductId), error.PropertyName);
            Assert.Equal("ProductId must be greater than 0.", error.ErrorMessage);
        }

        //Con 0 saltan las dos reglas (NotEmpty y GreaterThan), porque 0 es el valor por defecto del int.
        [Fact]
        public void Validate_CustomerIdCero_DevuelveDosErrores()
        {
            //Arrange
            var request = new ProductToCustomerCommand
            {
                ProductId = 1,
                CustomerId = 0
            };

            //Act
            var resultado = _validator.Validate(request);

            //Assert: no es valido y los dos errores son del CustomerId.
            Assert.False(resultado.IsValid);
            var numeroDeErrores = resultado.Errors.Count;
            Assert.Equal(2, numeroDeErrores);

            var primerError = resultado.Errors[0];
            Assert.Equal(nameof(ProductToCustomerCommand.CustomerId), primerError.PropertyName);
            Assert.Equal("CustomerId is required.", primerError.ErrorMessage);

            var segundoError = resultado.Errors[1];
            Assert.Equal(nameof(ProductToCustomerCommand.CustomerId), segundoError.PropertyName);
            Assert.Equal("CustomerId must be greater than 0.", segundoError.ErrorMessage);
        }

        //Cada fila: un CustomerId negativo, incluido el limite inferior del int.
        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        [InlineData(int.MinValue)]
        public void Validate_CustomerIdNegativo_DevuelveError(int customerId)
        {
            //Arrange
            var request = new ProductToCustomerCommand
            {
                ProductId = 1,
                CustomerId = customerId
            };

            //Act
            var resultado = _validator.Validate(request);

            //Assert: no es valido y hay un unico error, el del CustomerId.
            Assert.False(resultado.IsValid);
            var numeroDeErrores = resultado.Errors.Count;
            Assert.Equal(1, numeroDeErrores);

            var error = resultado.Errors[0];
            Assert.Equal(nameof(ProductToCustomerCommand.CustomerId), error.PropertyName);
            Assert.Equal("CustomerId must be greater than 0.", error.ErrorMessage);
        }

        [Fact]
        public void Validate_AmbosIdsInvalidos_DevuelveErroresDeLosDos()
        {
            //Arrange
            var request = new ProductToCustomerCommand
            {
                ProductId = -1,
                CustomerId = -1
            };

            //Act
            var resultado = _validator.Validate(request);

            //Assert: un error por cada Id.
            Assert.False(resultado.IsValid);
            var numeroDeErrores = resultado.Errors.Count;
            Assert.Equal(2, numeroDeErrores);

            var errorDeProduct = resultado.Errors[0];
            Assert.Equal(nameof(ProductToCustomerCommand.ProductId), errorDeProduct.PropertyName);
            Assert.Equal("ProductId must be greater than 0.", errorDeProduct.ErrorMessage);

            var errorDeCustomer = resultado.Errors[1];
            Assert.Equal(nameof(ProductToCustomerCommand.CustomerId), errorDeCustomer.PropertyName);
            Assert.Equal("CustomerId must be greater than 0.", errorDeCustomer.ErrorMessage);
        }

        //Un comando sin rellenar (los dos Id a 0, el valor por defecto) incumple las dos reglas de cada Id.
        [Fact]
        public void Validate_CommandSinRellenar_DevuelveCuatroErrores()
        {
            //Arrange
            var request = new ProductToCustomerCommand();

            //Act
            var resultado = _validator.Validate(request);

            //Assert: dos errores por cada Id.
            Assert.False(resultado.IsValid);
            var numeroDeErrores = resultado.Errors.Count;
            Assert.Equal(4, numeroDeErrores);


            var primerError = resultado.Errors[0];
            Assert.Equal(nameof(ProductToCustomerCommand.ProductId), primerError.PropertyName);
            Assert.Equal("ProductId is required.", primerError.ErrorMessage);

            var segundoError = resultado.Errors[1];
            Assert.Equal(nameof(ProductToCustomerCommand.ProductId), segundoError.PropertyName);
            Assert.Equal("ProductId must be greater than 0.", segundoError.ErrorMessage);

            var tercerError = resultado.Errors[2];
            Assert.Equal(nameof(ProductToCustomerCommand.CustomerId), tercerError.PropertyName);
            Assert.Equal("CustomerId is required.", tercerError.ErrorMessage);

            var cuartoError = resultado.Errors[3];
            Assert.Equal(nameof(ProductToCustomerCommand.CustomerId), cuartoError.PropertyName);
            Assert.Equal("CustomerId must be greater than 0.", cuartoError.ErrorMessage);
        }

        //Un Id valido con el otro invalido solo da error en el invalido.
        [Fact]
        public void Validate_SoloProductIdInvalido_NoMarcaElCustomerId()
        {
            //Arrange
            var request = new ProductToCustomerCommand
            {
                ProductId = -5,
                CustomerId = 3
            };

            //Act
            var resultado = _validator.Validate(request);

            //Assert: no es valido y el unico error es el del ProductId, ninguno del CustomerId.
            Assert.False(resultado.IsValid);
            var numeroDeErrores = resultado.Errors.Count;
            Assert.Equal(1, numeroDeErrores);

            var error = resultado.Errors[0];
            Assert.Equal(nameof(ProductToCustomerCommand.ProductId), error.PropertyName);
        }

        //Cada fila: Ids validos, incluido el limite superior del int.
        [Theory]
        [InlineData(1, 1)]
        [InlineData(1, int.MaxValue)]
        [InlineData(int.MaxValue, 1)]
        [InlineData(int.MaxValue, int.MaxValue)]
        [InlineData(7, 5)]
        public void Validate_IdsPositivos_NoTieneErrores(int productId, int customerId)
        {
            //Arrange
            var request = new ProductToCustomerCommand
            {
                ProductId = productId,
                CustomerId = customerId
            };

            //Act
            var resultado = _validator.Validate(request);

            //Assert
            Assert.True(resultado.IsValid);
            var numeroDeErrores = resultado.Errors.Count;
            Assert.Equal(0, numeroDeErrores);
        }
    }
}
