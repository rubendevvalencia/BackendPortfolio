using Ecommerce.Transversal.Common;
using Ecommerce.Transversal.Common.Enums;
using Microsoft.AspNetCore.Http.Timeouts;
using System.Net;
using System.Text.Json;

namespace Ecommerce.Api.Modules.TimeOut
{
    public static class TimeOutExtensions
    {
        public static IServiceCollection AddTimeOut(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddRequestTimeouts(options =>
            {
                options.DefaultPolicy = BuildPolicy(TimeSpan.FromMilliseconds(1500));
                options.AddPolicy("CustomPolicy", BuildPolicy(TimeSpan.FromMilliseconds(2000)));
            });

            return services;
        }

        //RequestTimeoutsMiddleware corta la peticion antes de que llegue al controller: ToActionResult
        //nunca se ejecuta para este caso, asi que el cuerpo con forma de Response<T> hay que escribirlo aqui.
        private static RequestTimeoutPolicy BuildPolicy(TimeSpan timeout)
        {
            return new RequestTimeoutPolicy
            {
                Timeout = timeout,
                TimeoutStatusCode = (int)HttpStatusCode.GatewayTimeout,
                WriteTimeoutResponse = WriteTimeoutResponseAsync
            };
        }

        private static Task WriteTimeoutResponseAsync(HttpContext context)
        {
            context.Response.ContentType = "application/json";
            var response = Response<object>.Fail("La operación superó el tiempo límite de espera.", ErrorType.TimeOut);
            return JsonSerializer.SerializeAsync(context.Response.Body, response);
        }
    }
}
