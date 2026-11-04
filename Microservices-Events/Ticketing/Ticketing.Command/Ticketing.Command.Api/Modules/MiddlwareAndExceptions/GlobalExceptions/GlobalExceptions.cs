using System.Net;
using System.Text.Json;
using Ticketing.Command.Application.Common.Exceptions;
using Ticketing.Common.Common.Response;

namespace Ticketing.Command.Api.Modules.MiddlewareAndExceptions.GlobalExceptions
{
    public class GlobalExceptionHandler : IMiddleware
    {
        private ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            //El middleware gestiona todas las excepciones
            try
            {
                await next(context); //Si todo es correcto continuamos
            }
            catch(ValidationExceptionCustom ex)
            {
                //Excepciones personalizadas
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest; //Sin esto la respuesta sale con 200

                //Agrupamos los errores por propiedad, que es el formato de Response.Errors
                var errors = new Dictionary<string, List<string>>();
                foreach (var error in ex.Errors)
                {
                    string property = error.PropertyMessage ?? string.Empty;
                    string message = error.ErrorMessage ?? string.Empty;

                    if (!errors.ContainsKey(property)) errors[property] = new List<string>();
                    errors[property].Add(message);
                }

                var response = Response<object>.Invalid(errors);
                await JsonSerializer.SerializeAsync(context.Response.Body, response);
            }
            catch (Exception ex)
            {
                //Excepciones del sistema
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                _logger.LogError(ex, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);

                var response = Response<bool>.Fail("Unhandle exception");

                await JsonSerializer.SerializeAsync(context.Response.Body, response);
            }

        }
    }
    
}

