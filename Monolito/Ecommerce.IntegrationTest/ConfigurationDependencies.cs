using Ecommerce.Api.Controllers;
using Ecommerce.Api.Controllers.UserAuth.UserAuthControllerCqrs;
using Ecommerce.Application;
using Ecommerce.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Ecommerce.IntegrationTest
{
    public static class ConfigurationDependencies
    {
        public static IServiceCollection BuildDependencies(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
        {
            services.AddSingleton(configuration);
            services.AddLogging();

            services.AddApplicationServices();
            services.AddInfrastructureServices(configuration, environment);

            services.AddScoped<UserAuthController>();
            services.AddScoped<UserAuthControllerCqrs>();

            return services;
        }
    }
}
