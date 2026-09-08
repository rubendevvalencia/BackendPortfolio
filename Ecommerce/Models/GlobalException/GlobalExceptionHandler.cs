using Ecommerce.Transversal.Common;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ecommerce.Api.Models.GlobalException
{
    public class GlobalExceptionHandler : IMiddleware
    {
        private ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) => _logger = logger;

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            //El middleware gestiona todas las excepciones
            try
            {
                await next(context); //Si todo es correcto continuamos
            }
            catch (Exception ex)
            {
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                _logger.LogError(ex, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);

                var response = new Response<Object>()
                {
                    Message = ex.Message,
                };

                
                await JsonSerializer.SerializeAsync(context.Response.Body, response, 
                    new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase, // Convierte Propiedad a propiedad
                        WriteIndented = true,                             // Formatea el JSON (Pretty-print)
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, // Ignora valores nulos
                        AllowTrailingCommas = true,                       // Permite comas al final
                        PropertyNameCaseInsensitive = true                // Deserialización tolerante a mayúsculas
                    }
                );
            }

        }
    }
}
