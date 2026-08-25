using Ecommerce.Domain.Interface.IRepository;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.Domain.Main
{
    public static class ConfigureServices
    {
        public static IServiceCollection AddDomainServices(this IServiceCollection services)
        {
            // Register domain-level services, validators, domain event handlers, etc.
            // Currently no concrete domain services to register; keep centralized for future additions.
            return services;
        }
    }
}
