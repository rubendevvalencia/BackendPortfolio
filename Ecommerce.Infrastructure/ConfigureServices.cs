using Ecommerce.Domain.Entities.Jwt;
using Ecommerce.Domain.Interface.IRepository;
using Ecommerce.Domain.Interface.IRepository.Jwt;
using Ecommerce.Infrastructure.Data;
using Ecommerce.Infrastructure.Interceptors;
using Ecommerce.Infrastructure.Repository;
using Ecommerce.Infrastructure.Repository.Jwt;
using Microsoft.AspNetCore.Identity;
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
                options.UseSqlServer(configuration.GetConnectionString("EcommerceDb"),          //Cadena de conexión a la base de datos
                builder => builder
                    .MigrationsAssembly(typeof(DbContextEF).Assembly.FullName)                  //Configura la migración de la base de datos
                    .EnableRetryOnFailure()));                                                   //Reintenta automáticamente ante fallos transitorios
            services.AddScoped<ICustomerRepository, CustomerRepository>();
            services.AddScoped<ICustomerRepositoryUoW, CustomerRepositoryUoW>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<AuditableEntitySaveChangesInterceptor>();
            services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
            services.AddScoped<ICustomerReadRepository, CustomerReadRepository>();
            return services;
        }
    }
}
