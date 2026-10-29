using Ticketing.Command.Api.Modules.MiddlewareAndExceptions.GlobalExceptions;

namespace Ticketing.Command.Api.Modules.MiddlewareAndExceptions.Middleware
{
    public static class MiddlewareExtensions
    {
        public static IServiceCollection AddMiddleWareService(this IServiceCollection services)
        {
            services.AddTransient<GlobalExceptionHandler>();
            return services;
        }
        public static IApplicationBuilder UseMiddlewares(this IApplicationBuilder app)
        {
            return app.UseMiddleware<GlobalExceptionHandler>(); //Esto indica que se use un Middleware personalizado
        }

       
    }
    
}


