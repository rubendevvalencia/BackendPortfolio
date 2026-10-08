using Ecommerce.Application.Feature.Products.Commands.CreateProduct;

namespace Ecommerce.Test.ApplicationTest.Feature.Products.Commands.CreateProduct
{
    //Tests del validador de CreateProduct por si solo, sin handler ni dobles: se le pasa un comando y se mira el resultado.
    public class CreateProductValidatorTests
    {
        private readonly CreateProductValidator _validator = new CreateProductValidator();

        [Fact]
        public void Validate_CommandValido_NoTieneErrores()
        {
            //Arrange
            var request = new CreateProductCommand
            {
                Name = "Libro",
                Description = "Novela",
                Price = 10.5m,
                StockQuantity = 5,
                Category = 1
            };

            //Act
            var resultado = _validator.Validate(request);

            //Assert
            Assert.True(resultado.IsValid);
            var numeroDeErrores = resultado.Errors.Count;
            Assert.Equal(0, numeroDeErrores);
        }

        //Cada fila: un Name vacio, solo con espacios o nulo.
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Validate_NameVacio_DevuelveError(string? name)
        {
            //Arrange
            var request = new CreateProductCommand
            {
                Name = name,
                Description = "Novela",
                Price = 10.5m,
                StockQuantity = 5,
                Category = 1
            };

            //Act
            var resultado = _validator.Validate(request);

            //Assert: no es valido y hay un unico error, el del Name.
            Assert.False(resultado.IsValid);
            var numeroDeErrores = resultado.Errors.Count;
            Assert.Equal(1, numeroDeErrores);

            var error = resultado.Errors[0];
            Assert.Equal(nameof(CreateProductCommand.Name), error.PropertyName);
            Assert.Equal("Product name is required.", error.ErrorMessage);
        }

        [Fact]
        public void Validate_NameEnLongitudMaxima_EsValido()
        {
            //Arrange: exactamente 100 caracteres.
            var request = new CreateProductCommand
            {
                Name = new string('a', 100),
                Description = "Novela",
                Price = 10.5m,
                StockQuantity = 5,
                Category = 1
            };

            //Act
            var resultado = _validator.Validate(request);

            //Assert
            Assert.True(resultado.IsValid);
            var numeroDeErrores = resultado.Errors.Count;
            Assert.Equal(0, numeroDeErrores);
        }

        [Fact]
        public void Validate_NameDemasiadoLargo_DevuelveError()
        {
            //Arrange: un caracter mas del limite.
            var request = new CreateProductCommand
            {
                Name = new string('a', 101),
                Description = "Novela",
                Price = 10.5m,
                StockQuantity = 5,
                Category = 1
            };

            //Act
            var resultado = _validator.Validate(request);

            //Assert: no es valido y hay un unico error, el de la longitud.
            Assert.False(resultado.IsValid);
            var numeroDeErrores = resultado.Errors.Count;
            Assert.Equal(1, numeroDeErrores);

            var error = resultado.Errors[0];
            Assert.Equal(nameof(CreateProductCommand.Name), error.PropertyName);
            Assert.Equal("Product name cannot exceed 100 characters.", error.ErrorMessage);
        }

        //Cada fila: una Description vacia, solo con espacios o nula.
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Validate_DescriptionVacia_DevuelveError(string? description)
        {
            //Arrange
            var request = new CreateProductCommand
            {
                Name = "Libro",
                Description = description,
                Price = 10.5m,
                StockQuantity = 5,
                Category = 1
            };

            //Act
            var resultado = _validator.Validate(request);

            //Assert: no es valido y hay un unico error, el de la Description.
            Assert.False(resultado.IsValid);
            var numeroDeErrores = resultado.Errors.Count;
            Assert.Equal(1, numeroDeErrores);

            var error = resultado.Errors[0];
            Assert.Equal(nameof(CreateProductCommand.Description), error.PropertyName);
            Assert.Equal("Product description is required.", error.ErrorMessage);
        }

        [Fact]
        public void Validate_DescriptionEnLongitudMaxima_EsValida()
        {
            //Arrange: exactamente 500 caracteres.
            var request = new CreateProductCommand
            {
                Name = "Libro",
                Description = new string('a', 500),
                Price = 10.5m,
                StockQuantity = 5,
                Category = 1
            };

            //Act
            var resultado = _validator.Validate(request);

            //Assert
            Assert.True(resultado.IsValid);
            var numeroDeErrores = resultado.Errors.Count;
            Assert.Equal(0, numeroDeErrores);
        }

        [Fact]
        public void Validate_DescriptionDemasiadoLarga_DevuelveError()
        {
            //Arrange: un caracter mas del limite.
            var request = new CreateProductCommand
            {
                Name = "Libro",
                Description = new string('a', 501),
                Price = 10.5m,
                StockQuantity = 5,
                Category = 1
            };

            //Act
            var resultado = _validator.Validate(request);

            //Assert: no es valido y hay un unico error, el de la longitud.
            Assert.False(resultado.IsValid);
            var numeroDeErrores = resultado.Errors.Count;
            Assert.Equal(1, numeroDeErrores);

            var error = resultado.Errors[0];
            Assert.Equal(nameof(CreateProductCommand.Description), error.PropertyName);
            Assert.Equal("Product description cannot exceed 500 characters.", error.ErrorMessage);
        }

        //Cada fila: un Price que no es mayor que 0.
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-0.01)]
        public void Validate_PriceNoPositivo_DevuelveError(double price)
        {
            //Arrange: double en la fila porque decimal no se puede usar en un atributo.
            var request = new CreateProductCommand
            {
                Name = "Libro",
                Description = "Novela",
                Price = (decimal)price,
                StockQuantity = 5,
                Category = 1
            };

            //Act
            var resultado = _validator.Validate(request);

            //Assert: no es valido y hay un unico error, el del Price.
            Assert.False(resultado.IsValid);
            var numeroDeErrores = resultado.Errors.Count;
            Assert.Equal(1, numeroDeErrores);

            var error = resultado.Errors[0];
            Assert.Equal(nameof(CreateProductCommand.Price), error.PropertyName);
            Assert.Equal("Price must be greater than zero.", error.ErrorMessage);
        }

        //Cada fila: un Price positivo, incluido el minimo con decimales.
        [Theory]
        [InlineData(0.01)]
        [InlineData(1)]
        [InlineData(999999.99)]
        public void Validate_PricePositivo_EsValido(double price)
        {
            //Arrange
            var request = new CreateProductCommand
            {
                Name = "Libro",
                Description = "Novela",
                Price = (decimal)price,
                StockQuantity = 5,
                Category = 1
            };

            //Act
            var resultado = _validator.Validate(request);

            //Assert
            Assert.True(resultado.IsValid);
            var numeroDeErrores = resultado.Errors.Count;
            Assert.Equal(0, numeroDeErrores);
        }

        //Cada fila: un StockQuantity negativo.
        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        [InlineData(int.MinValue)]
        public void Validate_StockNegativo_DevuelveError(int stock)
        {
            //Arrange
            var request = new CreateProductCommand
            {
                Name = "Libro",
                Description = "Novela",
                Price = 10.5m,
                StockQuantity = stock,
                Category = 1
            };

            //Act
            var resultado = _validator.Validate(request);

            //Assert: no es valido y hay un unico error, el del StockQuantity.
            Assert.False(resultado.IsValid);
            var numeroDeErrores = resultado.Errors.Count;
            Assert.Equal(1, numeroDeErrores);

            var error = resultado.Errors[0];
            Assert.Equal(nameof(CreateProductCommand.StockQuantity), error.PropertyName);
            Assert.Equal("Stock quantity cannot be negative.", error.ErrorMessage);
        }

        //Cada fila: un StockQuantity valido. El 0 si se permite (producto sin existencias).
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(int.MaxValue)]
        public void Validate_StockNoNegativo_EsValido(int stock)
        {
            //Arrange
            var request = new CreateProductCommand
            {
                Name = "Libro",
                Description = "Novela",
                Price = 10.5m,
                StockQuantity = stock,
                Category = 1
            };

            //Act
            var resultado = _validator.Validate(request);

            //Assert
            Assert.True(resultado.IsValid);
            var numeroDeErrores = resultado.Errors.Count;
            Assert.Equal(0, numeroDeErrores);
        }

        //Cada fila: una Category que no es mayor que 0 (el 0 tampoco vale, aunque el mensaje hable de negativos).
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Validate_CategoryNoPositiva_DevuelveError(int category)
        {
            //Arrange
            var request = new CreateProductCommand
            {
                Name = "Libro",
                Description = "Novela",
                Price = 10.5m,
                StockQuantity = 5,
                Category = category
            };

            //Act
            var resultado = _validator.Validate(request);

            //Assert: no es valido y hay un unico error, el de la Category.
            Assert.False(resultado.IsValid);
            var numeroDeErrores = resultado.Errors.Count;
            Assert.Equal(1, numeroDeErrores);

            var error = resultado.Errors[0];
            Assert.Equal(nameof(CreateProductCommand.Category), error.PropertyName);
            Assert.Equal("Category cannot be negative.", error.ErrorMessage);
        }

        //Comportamiento actual, no el deseado: no se comprueba que la Category exista en el enum (el maximo es 5).
        [Fact]
        public void Validate_CategoryFueraDelEnum_PasaLaValidacion()
        {
            //Arrange
            var request = new CreateProductCommand
            {
                Name = "Libro",
                Description = "Novela",
                Price = 10.5m,
                StockQuantity = 5,
                Category = 99
            };

            //Act
            var resultado = _validator.Validate(request);

            //Assert
            Assert.True(resultado.IsValid);
        }

        //Un comando con todo vacio o a cero acumula un error por cada regla incumplida.
        [Fact]
        public void Validate_CommandSinRellenar_DevuelveUnErrorPorCampo()
        {
            //Arrange: Price y StockQuantity tienen 0 por defecto y Category es null.
            var request = new CreateProductCommand();

            //Act
            var resultado = _validator.Validate(request);

            //Assert: Name, Description y Price fallan. StockQuantity=0 es valido.
            Assert.False(resultado.IsValid);
            var numeroDeErrores = resultado.Errors.Count;
            Assert.Equal(3, numeroDeErrores);

            var errorDeName = resultado.Errors[0];
            Assert.Equal(nameof(CreateProductCommand.Name), errorDeName.PropertyName);

            var errorDeDescription = resultado.Errors[1];
            Assert.Equal(nameof(CreateProductCommand.Description), errorDeDescription.PropertyName);

            var errorDePrice = resultado.Errors[2];
            Assert.Equal(nameof(CreateProductCommand.Price), errorDePrice.PropertyName);
        }
    }
}
