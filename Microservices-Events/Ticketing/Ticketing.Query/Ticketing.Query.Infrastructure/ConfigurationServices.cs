using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ticketing.Query.Infrastructure.Data;

namespace Ticketing.Query.Infrastructure
{
    public static class ConfigurationServices
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
        {
            // Example: services.AddScoped<ICustomerRepository, CustomerRepository>();
            services.AddDbContext<TicketDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("PostgresConnection")));

            return services;
        }

    }

}


