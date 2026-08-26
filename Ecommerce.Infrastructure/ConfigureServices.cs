using Ecommerce.Domain.Interface.IRepository;
using Ecommerce.Infrastructure.Data;
using Ecommerce.Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.Infrastructure
{
    public static class ConfigureServices
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Register your infrastructure services here
            // Example: services.AddScoped<ICustomerRepository, CustomerRepository>();
            services.AddDbContext<DbContextEF>(options =>
                options.UseSqlServer(configuration.GetConnectionString("EcommerceDb")));
            services.AddScoped<ICustomerRepository, CustomerRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            return services;
        }
    }
}
