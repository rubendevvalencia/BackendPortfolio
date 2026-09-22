using Ecommerce.Application.Feature.Customers.Commands.UpdateCustomer;

namespace Ecommerce.Test.ApplicationTest.Feature.v3.Commands.UpdateCustomer
{
    //Tests del validador v3 por si solo, sin handler ni dobles: se le pasa un comando y se mira el resultado.
    //Cada fila de [InlineData] es una regla de UpdateCustomerValidator: si alguien cambia una regla, falla su fila.
    public class UpdateCustomerValidatorTests
    {
        private readonly UpdateCustomerValidator _validator = new UpdateCustomerValidator();

        [Fact]
        public void Validate_CommandValido_NoTieneErrores()
        {
            //Arrange
            var request = new UpdateCustomerCommand
            {
                Id = 1,
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

        //Cada fila: un Id que no es mayor que 0.
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Validate_IdNoPositivo_DevuelveError(int id)
        {
            //Arrange: un comando valido en el que solo cambiamos el Id.
            var request = new UpdateCustomerCommand
            {
                Id = id,
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

            //Assert: no es valido y hay un unico error, el del Id.
            Assert.False(resultado.IsValid);
            var numeroDeErrores = resultado.Errors.Count;
            Assert.Equal(1, numeroDeErrores);

            var error = resultado.Errors[0];
            Assert.Equal(nameof(UpdateCustomerCommand.Id), error.PropertyName);
            Assert.Equal("Id must be greater than 0", error.ErrorMessage);
        }

        //Cada fila: el campo que dejamos vacio y el mensaje que tiene que devolver el validador.
        [Theory]
        [InlineData(nameof(UpdateCustomerCommand.CompanyName), "Company name is required.")]
        [InlineData(nameof(UpdateCustomerCommand.ContactName), "Contact name is required.")]
        [InlineData(nameof(UpdateCustomerCommand.ContactTitle), "Contact title is required.")]
        [InlineData(nameof(UpdateCustomerCommand.Address), "Address is required.")]
        [InlineData(nameof(UpdateCustomerCommand.City), "City is required.")]
        [InlineData(nameof(UpdateCustomerCommand.Region), "Region is required.")]
        [InlineData(nameof(UpdateCustomerCommand.PostalCode), "Postal code is required.")]
        [InlineData(nameof(UpdateCustomerCommand.Country), "Country is required.")]
        [InlineData(nameof(UpdateCustomerCommand.Phone), "Phone is required.")]
        [InlineData(nameof(UpdateCustomerCommand.Fax), "Fax is required.")]
        public void Validate_CampoVacio_DevuelveError(string campo, string mensajeEsperado)
        {
            //Arrange: un comando valido en el que solo cambiamos el campo de la fila.
            var request = new UpdateCustomerCommand
            {
                Id = 1,
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
            var propiedad = typeof(UpdateCustomerCommand).GetProperty(campo)!;
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
        [InlineData(nameof(UpdateCustomerCommand.CompanyName), 100)]
        [InlineData(nameof(UpdateCustomerCommand.ContactName), 50)]
        [InlineData(nameof(UpdateCustomerCommand.ContactTitle), 50)]
        [InlineData(nameof(UpdateCustomerCommand.Address), 200)]
        [InlineData(nameof(UpdateCustomerCommand.City), 50)]
        [InlineData(nameof(UpdateCustomerCommand.Region), 50)]
        [InlineData(nameof(UpdateCustomerCommand.PostalCode), 20)]
        [InlineData(nameof(UpdateCustomerCommand.Country), 50)]
        [InlineData(nameof(UpdateCustomerCommand.Phone), 20)]
        [InlineData(nameof(UpdateCustomerCommand.Fax), 20)]
        public void Validate_CampoEnLongitudMaxima_EsValido(string campo, int longitudMaxima)
        {
            //Arrange
            var request = new UpdateCustomerCommand
            {
                Id = 1,
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

            //Un texto con exactamente la longitud maxima.
            var textoEnElLimite = new string('a', longitudMaxima);
            var propiedad = typeof(UpdateCustomerCommand).GetProperty(campo)!;
            propiedad.SetValue(request, textoEnElLimite);

            //Act
            var resultado = _validator.Validate(request);

            //Assert
            Assert.True(resultado.IsValid);
            var numeroDeErrores = resultado.Errors.Count;
            Assert.Equal(0, numeroDeErrores);
        }

        //Cada fila: el campo, su longitud maxima y el mensaje. Un caracter mas del limite tiene que fallar.
        [Theory]
        [InlineData(nameof(UpdateCustomerCommand.CompanyName), 100, "Company name cannot exceed 100 characters.")]
        [InlineData(nameof(UpdateCustomerCommand.ContactName), 50, "Contact name cannot exceed 50 characters.")]
        [InlineData(nameof(UpdateCustomerCommand.ContactTitle), 50, "Contact title cannot exceed 50 characters.")]
        [InlineData(nameof(UpdateCustomerCommand.Address), 200, "Address cannot exceed 200 characters.")]
        [InlineData(nameof(UpdateCustomerCommand.City), 50, "City cannot exceed 50 characters.")]
        [InlineData(nameof(UpdateCustomerCommand.Region), 50, "Region cannot exceed 50 characters.")]
        [InlineData(nameof(UpdateCustomerCommand.PostalCode), 20, "Postal code cannot exceed 20 characters.")]
        [InlineData(nameof(UpdateCustomerCommand.Country), 50, "Country cannot exceed 50 characters.")]
        [InlineData(nameof(UpdateCustomerCommand.Phone), 20, "Phone cannot exceed 20 characters.")]
        [InlineData(nameof(UpdateCustomerCommand.Fax), 20, "Fax cannot exceed 20 characters.")]
        public void Validate_CampoDemasiadoLargo_DevuelveError(string campo, int longitudMaxima, string mensajeEsperado)
        {
            //Arrange
            var request = new UpdateCustomerCommand
            {
                Id = 1,
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
            var propiedad = typeof(UpdateCustomerCommand).GetProperty(campo)!;
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
