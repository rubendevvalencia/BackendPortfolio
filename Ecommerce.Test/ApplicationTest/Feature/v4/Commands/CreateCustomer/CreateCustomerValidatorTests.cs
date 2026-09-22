using Ecommerce.Application.Feature.Customers.v4.Commands.CreateCustomer;

namespace Ecommerce.Test.ApplicationTest.Feature.v4.Commands.CreateCustomer
{
    //Tests del validador v4 por si solo, sin handler ni dobles: se le pasa un comando y se mira el resultado.
    //Cada fila de [InlineData] es una regla de CreateCustomerValidator: si alguien cambia una regla, falla su fila.
    public class CreateCustomerValidatorTests
    {
        private readonly CreateCustomerValidator _validator = new CreateCustomerValidator();

        [Fact]
        public void Validate_CommandValido_NoTieneErrores()
        {
            //Arrange
            var request = new CreateCustomerCommand
            {
                CompanyName = "Test",
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

            //Act
            var resultado = _validator.Validate(request);

            //Assert
            Assert.True(resultado.IsValid);
            var numeroDeErrores = resultado.Errors.Count;
            Assert.Equal(0, numeroDeErrores);
        }

        //Cada fila: el campo que dejamos vacio y el mensaje que tiene que devolver el validador.
        [Theory]
        [InlineData(nameof(CreateCustomerCommand.CompanyName), "Company name is required.")]
        [InlineData(nameof(CreateCustomerCommand.ContactName), "Contact name is required.")]
        [InlineData(nameof(CreateCustomerCommand.ContactTitle), "Contact title is required.")]
        [InlineData(nameof(CreateCustomerCommand.Address), "Address is required.")]
        [InlineData(nameof(CreateCustomerCommand.City), "City is required.")]
        [InlineData(nameof(CreateCustomerCommand.Region), "Region is required.")]
        [InlineData(nameof(CreateCustomerCommand.PostalCode), "Postal code is required.")]
        [InlineData(nameof(CreateCustomerCommand.Country), "Country is required.")]
        [InlineData(nameof(CreateCustomerCommand.Phone), "Phone is required.")]
        [InlineData(nameof(CreateCustomerCommand.Fax), "Fax is required.")]
        public void Validate_CampoVacio_DevuelveError(string campo, string mensajeEsperado)
        {
            //Arrange: un comando valido en el que solo cambiamos el campo de la fila.
            var request = new CreateCustomerCommand
            {
                CompanyName = "Test",
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

            //Buscamos la propiedad por su nombre y la dejamos vacia.
            var propiedad = typeof(CreateCustomerCommand).GetProperty(campo)!;
            propiedad.SetValue(request, "");

            //Act
            var resultado = _validator.Validate(request);

            //Assert: no es valido y hay un unico error, el del campo vacio.
            Assert.False(resultado.IsValid);
            var numeroDeErrores = resultado.Errors.Count;
            Assert.Equal(1, numeroDeErrores);

            var error = resultado.Errors[0];
            Assert.Equal(campo, error.PropertyName);
            Assert.Equal(mensajeEsperado, error.ErrorMessage);
        }

        //Cada fila: el campo y su longitud maxima permitida. Justo en el limite tiene que ser valido.
        [Theory]
        [InlineData(nameof(CreateCustomerCommand.CompanyName), 100)]
        [InlineData(nameof(CreateCustomerCommand.ContactName), 50)]
        [InlineData(nameof(CreateCustomerCommand.ContactTitle), 50)]
        [InlineData(nameof(CreateCustomerCommand.Address), 200)]
        [InlineData(nameof(CreateCustomerCommand.City), 50)]
        [InlineData(nameof(CreateCustomerCommand.Region), 50)]
        [InlineData(nameof(CreateCustomerCommand.PostalCode), 20)]
        [InlineData(nameof(CreateCustomerCommand.Country), 50)]
        [InlineData(nameof(CreateCustomerCommand.Phone), 20)]
        [InlineData(nameof(CreateCustomerCommand.Fax), 20)]
        public void Validate_CampoEnLongitudMaxima_EsValido(string campo, int longitudMaxima)
        {
            //Arrange
            var request = new CreateCustomerCommand
            {
                CompanyName = "Test",
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

            string result = "";
            for (int i = 0; i<longitudMaxima; i++) result += "a";
            var propiedad = typeof(CreateCustomerCommand).GetProperty(campo);
            propiedad.SetValue(request, result);

            //Act
            var resultado = _validator.Validate(request);

            //Assert
            Assert.True(resultado.IsValid);
            var numeroDeErrores = resultado.Errors.Count;
            Assert.Equal(0, numeroDeErrores);
        }

        //Cada fila: el campo, su longitud maxima y el mensaje. Un caracter mas del limite tiene que fallar.
        [Theory]
        [InlineData(nameof(CreateCustomerCommand.CompanyName), 100, "Company name cannot exceed 100 characters.")]
        [InlineData(nameof(CreateCustomerCommand.ContactName), 50, "Contact name cannot exceed 50 characters.")]
        [InlineData(nameof(CreateCustomerCommand.ContactTitle), 50, "Contact title cannot exceed 50 characters.")]
        [InlineData(nameof(CreateCustomerCommand.Address), 200, "Address cannot exceed 200 characters.")]
        [InlineData(nameof(CreateCustomerCommand.City), 50, "City cannot exceed 50 characters.")]
        [InlineData(nameof(CreateCustomerCommand.Region), 50, "Region cannot exceed 50 characters.")]
        [InlineData(nameof(CreateCustomerCommand.PostalCode), 20, "Postal code cannot exceed 20 characters.")]
        [InlineData(nameof(CreateCustomerCommand.Country), 50, "Country cannot exceed 50 characters.")]
        [InlineData(nameof(CreateCustomerCommand.Phone), 20, "Phone cannot exceed 20 characters.")]
        [InlineData(nameof(CreateCustomerCommand.Fax), 20, "Fax cannot exceed 20 characters.")]
        public void Validate_CampoDemasiadoLargo_DevuelveError(string campo, int longitudMaxima, string mensajeEsperado)
        {
            //Arrange
            var request = new CreateCustomerCommand
            {
                CompanyName = "Test",
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

            //Un texto con un caracter mas de la longitud maxima.
            var longitudExcedida = longitudMaxima + 1;
            var textoDemasiadoLargo = new string('a', longitudExcedida);
            var propiedad = typeof(CreateCustomerCommand).GetProperty(campo)!;
            propiedad.SetValue(request, textoDemasiadoLargo);

            //Act
            var resultado = _validator.Validate(request);

            //Assert: no es valido y hay un unico error, el de la longitud.
            Assert.False(resultado.IsValid);
            var numeroDeErrores = resultado.Errors.Count;
            Assert.Equal(1, numeroDeErrores);

            var error = resultado.Errors[0];
            Assert.Equal(campo, error.PropertyName);
            Assert.Equal(mensajeEsperado, error.ErrorMessage);
        }
    }
}
