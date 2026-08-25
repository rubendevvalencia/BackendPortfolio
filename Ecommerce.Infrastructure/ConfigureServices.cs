using Ecommerce.Domain.Interface.IRepository;
using Ecommerce.Infrastructure.Interface.IUoW;
using Ecommerce.Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.Infrastructure
{
    public static class ConfigureServices
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
        {
            // Register your infrastructure services here
            // Example: services.AddScoped<ICustomerRepository, CustomerRepository>();
            services.AddSingleton<DbContext>();
            services.AddScoped<ICustomerRepository, CustomerRepository>();
            services.AddScoped<ICustomerUoW, CustomerUoW>();
            return services;
        }
    }
}
