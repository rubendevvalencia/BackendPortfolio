using System.Text.Json;

namespace Ecommerce.Api.Modules.HealthCheck
{
    public static class HealthCheckExtensions
    {
        public static IServiceCollection AddHealthCheck(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHealthChecks()
                .AddSqlServer(configuration.GetConnectionString("EcommerceDb"), tags: new[] { "database" })
                .AddCheck<HealthCheckCustome>("HealthCheckCustom", tags: new[] {"custom"});
            
            return services;
        }
    }
}
