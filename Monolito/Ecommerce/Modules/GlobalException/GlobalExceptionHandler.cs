using Ecommerce.Application.Common.Behaviours.Exceptions;
using Ecommerce.Transversal.Common;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ecommerce.Api.Models.GlobalException
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
                await JsonSerializer.SerializeAsync(context.Response.Body,
                    new Response<Object>
                    {
                        Message = "Errores de validación",
                        Error = ex.Errors
                    });

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
