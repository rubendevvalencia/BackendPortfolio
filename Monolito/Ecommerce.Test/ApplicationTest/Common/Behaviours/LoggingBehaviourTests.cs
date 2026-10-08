using Ecommerce.Application.Common.Behaviours;
using Ecommerce.Application.Feature.Customers.v4.Commands.CreateCustomer;
using Ecommerce.Transversal.Common;
using Ecommerce.Transversal.Common.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Ecommerce.Test.ApplicationTest.Common.Behaviours
{
    //Tests del behaviour con un ILogger sustituido y un next hecho a mano que cuenta si se le llama.
    public class LoggingBehaviourTests
    {
        private readonly ILogger<LoggingBehaviour<CreateCustomerCommand, Response<bool>>> _logger = Substitute.For<ILogger<LoggingBehaviour<CreateCustomerCommand, Response<bool>>>>();
        private readonly LoggingBehaviour<CreateCustomerCommand, Response<bool>> _behaviour;

        public LoggingBehaviourTests()
        {
            _behaviour = new LoggingBehaviour<CreateCustomerCommand, Response<bool>>(_logger);
        }

        [Fact]
        public async Task Handle_RespuestaExitosa_LogueaDebugEInformation()
        {
            //Arrange
            var request = new CreateCustomerCommand();

            var vecesNext = 0;
            var respuestaDelHandler = Response<bool>.Success(true);
            RequestHandlerDelegate<Response<bool>> next = (CancellationToken token) =>
            {
                vecesNext++;
                return Task.FromResult(respuestaDelHandler);
            };

            //Act
            var response = await _behaviour.Handle(request, next, CancellationToken.None);

            //Assert: la peticion llega al handler y el behaviour devuelve su respuesta tal cual.
            Assert.Equal(1, vecesNext);
            Assert.Same(respuestaDelHandler, response);

            //Assert: el logger recibe dos entradas, la primera Debug y la segunda Information.
            var llamadas = _logger.ReceivedCalls().ToList();
            Assert.Equal(2, llamadas.Count);

            var nivelPrimera = (LogLevel)llamadas[0].GetArguments()[0];
            Assert.Equal(LogLevel.Debug, nivelPrimera);

            var nivelSegunda = (LogLevel)llamadas[1].GetArguments()[0];
            Assert.Equal(LogLevel.Information, nivelSegunda);
        }

        [Theory]
        [InlineData(ErrorType.Validation, LogLevel.Information)]
        [InlineData(ErrorType.NotFound, LogLevel.Information)]
        [InlineData(ErrorType.Duplicated, LogLevel.Warning)]
        [InlineData(ErrorType.Unexpected, LogLevel.Warning)]
        [InlineData(ErrorType.TimeOut, LogLevel.Warning)]
        [InlineData(ErrorType.Unauthorized, LogLevel.Warning)]
        [InlineData(ErrorType.InvalidOperation, LogLevel.Warning)]
        public async Task Handle_RespuestaFallida_LogueaEnElNivelSegunElErrorType(ErrorType errorType, LogLevel nivelEsperado)
        {
            //Arrange
            var request = new CreateCustomerCommand();

            var vecesNext = 0;
            var respuestaDelHandler = Response<bool>.Fail("Fallo de prueba", errorType);
            RequestHandlerDelegate<Response<bool>> next = (CancellationToken token) =>
            {
                vecesNext++;
                return Task.FromResult(respuestaDelHandler);
            };

            //Act
            var response = await _behaviour.Handle(request, next, CancellationToken.None);

            //Assert: el fallo no se traga ni se transforma, el behaviour solo deja rastro.
            Assert.Equal(1, vecesNext);
            Assert.Same(respuestaDelHandler, response);

            //Assert: la segunda entrada (la primera es el Debug) tiene el nivel que toca para ese ErrorType.
            var llamadas = _logger.ReceivedCalls().ToList();
            Assert.Equal(2, llamadas.Count);

            var nivelSegunda = (LogLevel)llamadas[1].GetArguments()[0];
            Assert.Equal(nivelEsperado, nivelSegunda);
        }

        [Fact]
        public async Task Handle_RespuestaQueNoEsResponse_SoloLogueaDebug()
        {
            //Arrange: la respuesta es un string, que no es un Response<T>, asi que no hay nada que clasificar.
            //Este caso usa otros tipos genericos, por eso lleva su propio logger y su propio behaviour.
            var request = Substitute.For<IRequest<string>>();

            var logger = Substitute.For<ILogger<LoggingBehaviour<IRequest<string>, string>>>();
            var behaviour = new LoggingBehaviour<IRequest<string>, string>(logger);

            var vecesNext = 0;
            var respuestaDelHandler = "respuesta";
            RequestHandlerDelegate<string> next = (CancellationToken token) =>
            {
                vecesNext++;
                return Task.FromResult(respuestaDelHandler);
            };

            //Act
            var response = await behaviour.Handle(request, next, CancellationToken.None);

            //Assert: la peticion llega al handler y el behaviour devuelve su respuesta tal cual.
            Assert.Equal(1, vecesNext);
            Assert.Equal(respuestaDelHandler, response);

            //Assert: el logger recibe una sola entrada, la del Debug previo.
            var llamadas = logger.ReceivedCalls().ToList();
            Assert.Single(llamadas);

            var nivelPrimera = (LogLevel)llamadas[0].GetArguments()[0];
            Assert.Equal(LogLevel.Debug, nivelPrimera);
        }
    }
}
