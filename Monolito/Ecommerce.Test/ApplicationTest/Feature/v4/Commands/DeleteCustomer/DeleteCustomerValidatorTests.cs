using Ecommerce.Application.Feature.Customers.v4.Commands.DeleteCustomer;

namespace Ecommerce.Test.ApplicationTest.Feature.v4.Commands.DeleteCustomer
{
    //Tests del validador v4 por si solo, sin handler ni dobles: se le pasa un comando y se mira el resultado.
    public class DeleteCustomerValidatorTests
    {
        private readonly DeleteCustomerValidator _validator = new DeleteCustomerValidator();

        [Fact]
        public void Validate_IdPositivo_NoTieneErrores()
        {
            //Arrange
            var request = new DeleteCustomerCommand { Id = 1 };

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
            //Arrange
            var request = new DeleteCustomerCommand { Id = id };

            //Act
            var resultado = _validator.Validate(request);

            //Assert: no es valido y hay un unico error, el del Id.
            Assert.False(resultado.IsValid);
            var numeroDeErrores = resultado.Errors.Count;
            Assert.Equal(1, numeroDeErrores);

            var error = resultado.Errors[0];
            Assert.Equal(nameof(DeleteCustomerCommand.Id), error.PropertyName);
            Assert.Equal("Id must be greater than 0", error.ErrorMessage);
        }
    }
}
