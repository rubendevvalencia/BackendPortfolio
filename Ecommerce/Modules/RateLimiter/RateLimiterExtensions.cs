using Ecommerce.Application.Common.Behaviours.Exceptions;
using Ecommerce.Transversal.Common;
using Microsoft.AspNetCore.RateLimiting;

namespace Ecommerce.Api.Modules.RateLimiter
{
    public static class RateLimiterExtensions
    {
        public static IServiceCollection AddRateLimiting(this IServiceCollection services, IConfiguration configuration)
        {
            //Ratelimiter simple para que no hagan ataques masivos, de momento no se encuentra particionado debdido a que tenemos un ejemplo simple

            var fixedWindowPolicy = "fixedWindow";

            //Se lee una sola vez: cada llamada a Configuration vuelve a parsear las tres claves.
            RateLimiterSettings settings = RateLimiterConfiguration.Configuration(configuration);

            services.AddRateLimiter(configureOptions =>
            {
                configureOptions.AddFixedWindowLimiter(policyName: fixedWindowPolicy, fixedWindow =>
                {
                    fixedWindow.PermitLimit = settings.PermitLimit;                                                     //Nº de peticiones máximo por ventana de tiempo
                    fixedWindow.Window = settings.Window;                                                               //Tiempo de la ventana
                    fixedWindow.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;  //Gestión de la cola (FIFO)
                    fixedWindow.QueueLimit = settings.QueueLimit;                                                       //Nº de peticiones que esperan en la cola (sin respuesta) a la siguiente ventana cuando no quedan permisos
                });
                configureOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests; //Responde 429 con demasiadas request
            });

            return services;
        }
    }
}
