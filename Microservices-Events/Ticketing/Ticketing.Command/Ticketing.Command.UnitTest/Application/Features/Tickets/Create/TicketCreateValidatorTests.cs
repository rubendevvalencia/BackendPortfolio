using FluentValidation.Results;
using Ticketing.Command.Application.Features.Tickets.Create;

namespace Ticketing.Command.UnitTest.Application.Features.Tickets.Create
{
    //Unitario de lógica sobre el validador: son las combinaciones de sus reglas, sin pasar por el handler
    public class TicketCreateValidatorTests
    {
        public static TheoryData<string, TicketCreateCommand> ValidCommands()
        {
            TheoryData<string, TicketCreateCommand> data = new();
            data.Add("todo válido", new TicketCreateCommand { UserName = "ana@empresa.com", ErrorType = "Timeout", ErrorDetail = "Timeout al pagar" });
            data.Add("UserName de 100 caracteres", new TicketCreateCommand { UserName = new string('a', 100), ErrorType = "Timeout", ErrorDetail = "Timeout al pagar" });
            data.Add("ErrorType de 100 caracteres", new TicketCreateCommand { UserName = "ana@empresa.com", ErrorType = new string('a', 100), ErrorDetail = "Timeout al pagar" });
            data.Add("ErrorDetail de 400 caracteres", new TicketCreateCommand { UserName = "ana@empresa.com", ErrorType = "Timeout", ErrorDetail = new string('a', 400) });
            return data;
        }

        public static TheoryData<string, TicketCreateCommand, string> InvalidCommands()
        {
            TheoryData<string, TicketCreateCommand, string> data = new();
            data.Add("UserName nulo", new TicketCreateCommand { UserName = null, ErrorType = "Timeout", ErrorDetail = "Timeout al pagar" }, "UserName");
            data.Add("UserName vacío", new TicketCreateCommand { UserName = "", ErrorType = "Timeout", ErrorDetail = "Timeout al pagar" }, "UserName");
            data.Add("UserName solo con espacios", new TicketCreateCommand { UserName = "   ", ErrorType = "Timeout", ErrorDetail = "Timeout al pagar" }, "UserName");
            data.Add("UserName de 101 caracteres", new TicketCreateCommand { UserName = new string('a', 101), ErrorType = "Timeout", ErrorDetail = "Timeout al pagar" }, "UserName");
            data.Add("ErrorType nulo", new TicketCreateCommand { UserName = "ana@empresa.com", ErrorType = null, ErrorDetail = "Timeout al pagar" }, "ErrorType");
            data.Add("ErrorType vacío", new TicketCreateCommand { UserName = "ana@empresa.com", ErrorType = "", ErrorDetail = "Timeout al pagar" }, "ErrorType");
            data.Add("ErrorType solo con espacios", new TicketCreateCommand { UserName = "ana@empresa.com", ErrorType = "   ", ErrorDetail = "Timeout al pagar" }, "ErrorType");
            data.Add("ErrorType de 101 caracteres", new TicketCreateCommand { UserName = "ana@empresa.com", ErrorType = new string('a', 101), ErrorDetail = "Timeout al pagar" }, "ErrorType");
            data.Add("ErrorDetail nulo", new TicketCreateCommand { UserName = "ana@empresa.com", ErrorType = "Timeout", ErrorDetail = null }, "ErrorDetail");
            data.Add("ErrorDetail vacío", new TicketCreateCommand { UserName = "ana@empresa.com", ErrorType = "Timeout", ErrorDetail = "" }, "ErrorDetail");
            data.Add("ErrorDetail solo con espacios", new TicketCreateCommand { UserName = "ana@empresa.com", ErrorType = "Timeout", ErrorDetail = "   " }, "ErrorDetail");
            data.Add("ErrorDetail de 401 caracteres", new TicketCreateCommand { UserName = "ana@empresa.com", ErrorType = "Timeout", ErrorDetail = new string('a', 401) }, "ErrorDetail");
            return data;
        }

        [Theory]
        [MemberData(nameof(ValidCommands))]
        public void Validate_DatosValidos_EsValido(string scenario, TicketCreateCommand command)
        {
            //Arrange: validador sin dependencias
            TicketCreateValidator validator = new TicketCreateValidator();

            //Act
            ValidationResult result = validator.Validate(command);

            //Assert: sin ningún error
            Assert.True(result.IsValid, scenario);
            Assert.Empty(result.Errors);
        }

        [Theory]
        [MemberData(nameof(InvalidCommands))]
        public void Validate_UnCampoInvalido_FallaSoloEnEsaPropiedad(string scenario, TicketCreateCommand command, string expectedProperty)
        {
            //Arrange: validador sin dependencias
            TicketCreateValidator validator = new TicketCreateValidator();

            //Act
            ValidationResult result = validator.Validate(command);

            //Assert: un único error, en la propiedad de la regla rota
            Assert.False(result.IsValid, scenario);
            ValidationFailure failure = Assert.Single(result.Errors);
            Assert.Equal(expectedProperty, failure.PropertyName);
        }

        [Fact]
        public void Validate_TodosLosCamposVacios_ReportaLasTresPropiedades()
        {
            //Arrange: command sin ningún dato
            TicketCreateValidator validator = new TicketCreateValidator();
            TicketCreateCommand command = new TicketCreateCommand { UserName = null, ErrorType = null, ErrorDetail = null };

            //Act
            ValidationResult result = validator.Validate(command);

            //Assert: devuelve los errores de todas las propiedades, no solo el primero
            Assert.False(result.IsValid);
            List<string> properties = result.Errors.Select(error => error.PropertyName).Distinct().OrderBy(name => name).ToList();
            Assert.Equal(new List<string> { "ErrorDetail", "ErrorType", "UserName" }, properties);
        }
    }
}
