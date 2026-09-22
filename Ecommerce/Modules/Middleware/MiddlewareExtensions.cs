using Ecommerce.Api.Models.GlobalException;

namespace Ecommerce.Api.Models.Middleware
{
    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseMiddlewares(this IApplicationBuilder app)
        {
            return app.UseMiddleware<GlobalExceptionHandler>(); //Esto indica que se use un Middleware personalizado
        }

        public static IServiceCollection AddMiddleWareService(this IServiceCollection services)
        {
            services.AddTransient<GlobalExceptionHandler>();
            return services;
        }
    }
}
