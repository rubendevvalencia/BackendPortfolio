using Ecommerce.Transversal.Common;
using System.Net;
using System.Text.Json;

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
                _logger.LogError($"Exception details: {ex.Message.ToString()}");

                var response = new Response<Object>()
                {
                    Message = ex.Message,
                };

                await JsonSerializer.SerializeAsync(context.Response.Body, response);

            }

        }
    }
}
