using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Ticketing.Query.Infrastructure
{
    public static class ConfigurationServices
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
        {
            // Example: services.AddScoped<ICustomerRepository, CustomerRepository>();
            services.AddDbContext<DbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("EcommerceDb"),          //Cadena de conexión a la base de datos
                builder => builder
                    .MigrationsAssembly(typeof(DbContextEF).Assembly.FullName)                  //Configura la migración de la base de datos
                    .EnableRetryOnFailure())
                    .EnableSensitiveDataLogging(environment.IsDevelopment()));
        }

    }

}


