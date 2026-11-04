using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Ticketing.Command.Api.Modules.MiddlewareAndExceptions.GlobalExceptions;
using Ticketing.Command.Application.Common.Exceptions;
using Ticketing.Common.Common.Response;
using Ticketing.Common.Common.Response.Enums;

namespace Ticketing.Command.UnitTest.Api.Modules.MiddlewareAndExceptions.GlobalExceptions
{
    //Unitario de componente: el middleware traduce excepciones a respuesta HTTP y la salida es la propia respuesta, sin dependencias que simular
    public class GlobalExceptionHandlerTests
    {
        //Errores de validación genéricos, sin ligarlos a ninguna feature; los tests de 400, JSON y tipo no necesitan el esperado
        public static TheoryData<string, List<BaseError>> ValidationErrors()
        {
            TheoryData<string, List<BaseError>> data = new();
            data.Add(
                "un error en una propiedad",
                new List<BaseError>
                {
                    new BaseError { PropertyMessage = "PropertyA", ErrorMessage = "A1" }
                });
            data.Add(
                "dos errores en la misma propiedad",
                new List<BaseError>
                {
                    new BaseError { PropertyMessage = "PropertyA", ErrorMessage = "A1" },
                    new BaseError { PropertyMessage = "PropertyA", ErrorMessage = "A2" }
                });
            data.Add(
                "errores en dos propiedades",
                new List<BaseError>
                {
                    new BaseError { PropertyMessage = "PropertyA", ErrorMessage = "A1" },
                    new BaseError { PropertyMessage = "PropertyB", ErrorMessage = "B1" }
                });
            data.Add(
                "errores de una propiedad intercalados con otra",
                new List<BaseError>
                {
                    new BaseError { PropertyMessage = "PropertyA", ErrorMessage = "A1" },
                    new BaseError { PropertyMessage = "PropertyB", ErrorMessage = "B1" },
                    new BaseError { PropertyMessage = "PropertyA", ErrorMessage = "A2" }
                });
            return data;
        }

        //Las mismas filas que ValidationErrors, con el esperado escrito a mano para el test de agrupación
        public static TheoryData<string, List<BaseError>, Dictionary<string, List<string>>> ValidationFailures()
        {
            TheoryData<string, List<BaseError>, Dictionary<string, List<string>>> data = new();
            data.Add(
                "un error en una propiedad",
                new List<BaseError>
                {
                    new BaseError { PropertyMessage = "PropertyA", ErrorMessage = "A1" }
                },
                new Dictionary<string, List<string>>
                {
                    ["PropertyA"] = new List<string> { "A1" }
                });
            data.Add(
                "dos errores en la misma propiedad",
                new List<BaseError>
                {
                    new BaseError { PropertyMessage = "PropertyA", ErrorMessage = "A1" },
                    new BaseError { PropertyMessage = "PropertyA", ErrorMessage = "A2" }
                },
                new Dictionary<string, List<string>>
                {
                    ["PropertyA"] = new List<string> { "A1", "A2" }
                });
            data.Add(
                "errores en dos propiedades",
                new List<BaseError>
                {
                    new BaseError { PropertyMessage = "PropertyA", ErrorMessage = "A1" },
                    new BaseError { PropertyMessage = "PropertyB", ErrorMessage = "B1" }
                },
                new Dictionary<string, List<string>>
                {
                    ["PropertyA"] = new List<string> { "A1" },
                    ["PropertyB"] = new List<string> { "B1" }
                });
            data.Add(
                "errores de una propiedad intercalados con otra",
                new List<BaseError>
                {
                    new BaseError { PropertyMessage = "PropertyA", ErrorMessage = "A1" },
                    new BaseError { PropertyMessage = "PropertyB", ErrorMessage = "B1" },
                    new BaseError { PropertyMessage = "PropertyA", ErrorMessage = "A2" }
                },
                new Dictionary<string, List<string>>
                {
                    ["PropertyA"] = new List<string> { "A1", "A2" },
                    ["PropertyB"] = new List<string> { "B1" }
                });
            return data;
        }

        //Todas las excepciones llevan el mismo dato sensible en el mensaje para el test de fuga
        public static TheoryData<string, Exception> UnexpectedExceptions()
        {
            TheoryData<string, Exception> data = new();
            data.Add("InvalidOperationException", new InvalidOperationException("Password=secreto"));
            data.Add("NullReferenceException", new NullReferenceException("Password=secreto"));
            data.Add("TimeoutException", new TimeoutException("Password=secreto"));
            return data;
        }

        [Theory]
        [InlineData(201)]
        [InlineData(404)]
        public async Task InvokeAsync_SinExcepcion_RespetaElEstadoYNoEscribeCuerpo(int statusCode)
        {
            //Arrange: el siguiente eslabón responde con su propio estado y no falla
            GlobalExceptionHandler handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
            DefaultHttpContext httpContext = new DefaultHttpContext();
            httpContext.Response.Body = new MemoryStream();
            RequestDelegate next = (HttpContext requestContext) =>
            {
                requestContext.Response.StatusCode = statusCode;
                return Task.CompletedTask;
            };

            //Act
            await handler.InvokeAsync(httpContext, next);

            //Assert: el estado es el del eslabón y el middleware no añade nada al cuerpo
            Assert.Equal(statusCode, httpContext.Response.StatusCode);
            Assert.Equal(0, httpContext.Response.Body.Length);
        }

        [Theory]
        [MemberData(nameof(ValidationErrors))]
        public async Task InvokeAsync_ValidationExceptionCustom_Responde400(string scenario, List<BaseError> errors)
        {
            //Arrange: el siguiente eslabón lanza un fallo de validación con los errores de la fila
            GlobalExceptionHandler handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
            DefaultHttpContext httpContext = new DefaultHttpContext();
            httpContext.Response.Body = new MemoryStream();
            ValidationExceptionCustom exception = new ValidationExceptionCustom(errors);
            //El siguiente eslabón del pipeline falla lanzando la excepción de arriba
            RequestDelegate next = (HttpContext requestContext) => throw exception;

            //Act
            await handler.InvokeAsync(httpContext, next);

            //Assert
            int statusCode = httpContext.Response.StatusCode;
            Assert.True(statusCode == 400, $"{scenario}: estado {statusCode}");
        }

        [Theory]
        [MemberData(nameof(ValidationErrors))]
        public async Task InvokeAsync_ValidationExceptionCustom_RespondeJson(string scenario, List<BaseError> errors)
        {
            //Arrange: el siguiente eslabón lanza un fallo de validación con los errores de la fila
            GlobalExceptionHandler handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
            DefaultHttpContext httpContext = new DefaultHttpContext();
            httpContext.Response.Body = new MemoryStream();
            ValidationExceptionCustom exception = new ValidationExceptionCustom(errors);
            //El siguiente eslabón del pipeline falla lanzando la excepción de arriba
            RequestDelegate next = (HttpContext requestContext) => throw exception;

            //Act
            await handler.InvokeAsync(httpContext, next);

            //Assert: el tipo de contenido es JSON, con o sin charset
            string? contentType = httpContext.Response.ContentType;
            Assert.NotNull(contentType);
            Assert.True(contentType.StartsWith("application/json"), $"{scenario}: tipo de contenido {contentType}");
        }

        [Theory]
        [MemberData(nameof(ValidationErrors))]
        public async Task InvokeAsync_ValidationExceptionCustom_CuerpoIndicaFalloDeValidacion(string scenario, List<BaseError> errors)
        {
            //Arrange: el siguiente eslabón lanza un fallo de validación con los errores de la fila
            GlobalExceptionHandler handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
            DefaultHttpContext httpContext = new DefaultHttpContext();
            httpContext.Response.Body = new MemoryStream();
            ValidationExceptionCustom exception = new ValidationExceptionCustom(errors);
            //El siguiente eslabón del pipeline falla lanzando la excepción de arriba
            RequestDelegate next = (HttpContext requestContext) => throw exception;

            //Act
            await handler.InvokeAsync(httpContext, next);

            //Assert: respuesta fallida de tipo Validation
            httpContext.Response.Body.Position = 0;
            JsonSerializerOptions options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            options.Converters.Add(new JsonStringEnumConverter());
            Response<object>? response = JsonSerializer.Deserialize<Response<object>>(httpContext.Response.Body, options);
            Assert.NotNull(response);
            Assert.False(response.IsSuccess, scenario);
            Assert.Equal(ErrorType.Validation, response.ErrorType);
        }

        [Theory]
        [MemberData(nameof(ValidationFailures))]
        public async Task InvokeAsync_ValidationExceptionCustom_DevuelveLosErroresAgrupadosPorPropiedad(
            string scenario,
            List<BaseError> errors,
            Dictionary<string, List<string>> expectedErrors)
        {
            //Arrange: el siguiente eslabón lanza un fallo de validación con los errores de la fila
            GlobalExceptionHandler handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
            DefaultHttpContext httpContext = new DefaultHttpContext();
            httpContext.Response.Body = new MemoryStream();
            ValidationExceptionCustom exception = new ValidationExceptionCustom(errors);
            //El siguiente eslabón del pipeline falla lanzando la excepción de arriba
            RequestDelegate next = (HttpContext requestContext) => throw exception;
            

            //Act
            await handler.InvokeAsync(httpContext, next);

            //Assert: un mensaje por error, agrupado bajo su propiedad y en el orden recibido
            httpContext.Response.Body.Position = 0;
            JsonSerializerOptions options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            options.Converters.Add(new JsonStringEnumConverter());
            Response<object>? response = JsonSerializer.Deserialize<Response<object>>(httpContext.Response.Body, options);
            Assert.NotNull(response);
            Assert.True(expectedErrors.Count == response.Errors.Count, scenario);
            Assert.Equal(expectedErrors, response.Errors);
        }

        [Theory]
        [MemberData(nameof(UnexpectedExceptions))]
        public async Task InvokeAsync_ExcepcionNoControlada_Responde500(string scenario, Exception exception)
        {
            //Arrange: el siguiente eslabón lanza una excepción que no es de validación
            GlobalExceptionHandler handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
            DefaultHttpContext httpContext = new DefaultHttpContext();
            httpContext.Response.Body = new MemoryStream();
            //El siguiente eslabón del pipeline falla lanzando la excepción de arriba
            RequestDelegate next = (HttpContext requestContext) => throw exception;

            //Act
            await handler.InvokeAsync(httpContext, next);

            //Assert
            int statusCode = httpContext.Response.StatusCode;
            Assert.True(statusCode == 500, $"{scenario}: estado {statusCode}");
        }

        [Theory]
        [MemberData(nameof(UnexpectedExceptions))]
        public async Task InvokeAsync_ExcepcionNoControlada_RespondeJson(string scenario, Exception exception)
        {
            //Arrange: el siguiente eslabón lanza una excepción que no es de validación
            GlobalExceptionHandler handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
            DefaultHttpContext httpContext = new DefaultHttpContext();
            httpContext.Response.Body = new MemoryStream();
            //El siguiente eslabón del pipeline falla lanzando la excepción de arriba
            RequestDelegate next = (HttpContext requestContext) => throw exception;

            //Act
            await handler.InvokeAsync(httpContext, next);

            //Assert: el tipo de contenido es JSON, con o sin charset
            string? contentType = httpContext.Response.ContentType;
            Assert.NotNull(contentType);
            Assert.True(contentType.StartsWith("application/json"), $"{scenario}: tipo de contenido {contentType}");
        }

        [Theory]
        [MemberData(nameof(UnexpectedExceptions))]
        public async Task InvokeAsync_ExcepcionNoControlada_CuerpoIndicaFalloInesperado(string scenario, Exception exception)
        {
            //Arrange: el siguiente eslabón lanza una excepción que no es de validación
            GlobalExceptionHandler handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
            DefaultHttpContext httpContext = new DefaultHttpContext();
            httpContext.Response.Body = new MemoryStream();
            //El siguiente eslabón del pipeline falla lanzando la excepción de arriba
            RequestDelegate next = (HttpContext requestContext) => throw exception;

            //Act
            await handler.InvokeAsync(httpContext, next);

            //Assert: respuesta fallida de tipo Unexpected, sin errores de validación
            httpContext.Response.Body.Position = 0;
            JsonSerializerOptions options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            options.Converters.Add(new JsonStringEnumConverter());
            Response<object>? response = JsonSerializer.Deserialize<Response<object>>(httpContext.Response.Body, options);
            Assert.NotNull(response);
            Assert.False(response.IsSuccess, scenario);
            Assert.Equal(ErrorType.Unexpected, response.ErrorType);
            Assert.Empty(response.Errors);
        }

        [Theory]
        [MemberData(nameof(UnexpectedExceptions))]
        public async Task InvokeAsync_ExcepcionNoControlada_NoFiltraElMensajeNiElTipoDeLaExcepcion(string scenario, Exception exception)
        {
            //Arrange: la excepción lleva un dato sensible en su mensaje
            GlobalExceptionHandler handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
            DefaultHttpContext httpContext = new DefaultHttpContext();
            httpContext.Response.Body = new MemoryStream();
            //El siguiente eslabón del pipeline falla lanzando la excepción de arriba
            RequestDelegate next = (HttpContext requestContext) => throw exception;

            //Act
            await handler.InvokeAsync(httpContext, next);

            //Assert: el cliente no recibe ni el mensaje ni el tipo de la excepción
            httpContext.Response.Body.Position = 0;
            StreamReader reader = new StreamReader(httpContext.Response.Body);
            string body = await reader.ReadToEndAsync();
            string exceptionTypeName = exception.GetType().Name;
            Assert.False(body.Contains("secreto"), $"{scenario}: el cuerpo contiene el mensaje");
            Assert.False(body.Contains(exceptionTypeName), $"{scenario}: el cuerpo contiene el tipo");
        }
    }
}
