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
            TimeSpan windowTime = TimeSpan.Zero;
            TimeSpan.TryParse(configuration["RateLimiting:Window"], out windowTime);
            int queueLimit = 0;
            int.TryParse(configuration["RateLimiting:QueueLimit"], out queueLimit);

            if (permitLimit == 0 || windowTime == TimeSpan.Zero /*|| queueLimit == 0*/)
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
                    fixedWindow.PermitLimit = permitLimit;
                    fixedWindow.Window = windowTime;
                    fixedWindow.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
                    fixedWindow.QueueLimit = queueLimit; 
                });

                configureOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests; //Responde 429 con demasiadas request
            });

            return services;
        }
    }
}
