using Ecommerce.Application.Feature.Customers.v4.Queries.GetCustomerQuery;

namespace Ecommerce.Test.ApplicationTest.Feature.v4.Queries.GetCustomer
{
    //Tests del validador v4 por si solo, sin handler ni dobles: se le pasa una query y se mira el resultado.
    public class GetCustomerValidatorTests
    {
        private readonly GetCustomerValidator _validator = new GetCustomerValidator();

        [Fact]
        public void Validate_IdPositivo_NoTieneErrores()
        {
            //Arrange
            var request = new GetCustomerQuery { Id = 1 };

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
            var request = new GetCustomerQuery { Id = id };

            //Act
            var resultado = _validator.Validate(request);

            //Assert: no es valido y hay un unico error, el del Id.
            Assert.False(resultado.IsValid);
            var numeroDeErrores = resultado.Errors.Count;
            Assert.Equal(1, numeroDeErrores);

            var error = resultado.Errors[0];
            Assert.Equal(nameof(GetCustomerQuery.Id), error.PropertyName);
            Assert.Equal("Id must be greater than 0", error.ErrorMessage);
        }
    }
}
