using Ecommerce.Domain.Interface.IService;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.Domain.MainService
{
    public static class ConfigureServices
    {
        public static IServiceCollection AddDomainServices(this IServiceCollection services)
        {
            // Register domain-level services, validators, domain event handlers, etc.
            services.AddScoped<ICustomerService, CustomerService>();
            return services;
        }
    }
}
