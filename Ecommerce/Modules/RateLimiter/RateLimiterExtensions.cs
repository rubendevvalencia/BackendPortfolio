using Ecommerce.Application.Common.Behaviours.Exceptions;
using Ecommerce.Transversal.Common;
using Microsoft.AspNetCore.RateLimiting;

namespace Ecommerce.Api.Modules.RateLimiter
{
    public static class RateLimiterExtensions
    {
        public static IServiceCollection AddRateLimiting(this IServiceCollection services, IConfiguration configuration)
        {
            var fixedWindowPolicy = "fixedWindow";
            int permitLimit = 0;
            int.TryParse(configuration["RateLimiting:PermitLimit"], out permitLimit);
            int windowTime = 0;
            int.TryParse(configuration["RateLimiting:Window"], out windowTime);
            int queueLimit = 0;
            int.TryParse(configuration["RateLimiting:QueueLimit"], out queueLimit);

            if (permitLimit == 0 || windowTime == 0 || queueLimit == 0)
                throw new ValidationExceptionCustom(
                    new List<BaseError>
                    {
                        new() {PropertyMessage = "RateLimiting-Configuration", ErrorMessage = "Incorrect Data" }
                    }
                );


            services.AddRateLimiter(configureOptions =>
            {
                configureOptions.AddFixedWindowLimiter(policyName: fixedWindowPolicy, fixedWindow =>
                {
                    fixedWindow.PermitLimit = permitLimit;                                                              //Nº de peticiones máximo por ventana de tiempo
                    fixedWindow.Window = TimeSpan.FromSeconds(windowTime);                                              //Tiempo de la ventana
                    fixedWindow.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;  //Gestión de la cola (FIFO)
                    fixedWindow.QueueLimit = queueLimit;                                                                //Nº de peticiones que esperan en la cola (sin respuesta) a la siguiente ventana cuando no quedan permisos
                });

                configureOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests; //Responde 429 con demasiadas request
            });

            return services;
        }
    }
}
