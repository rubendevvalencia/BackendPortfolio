using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ticketing.Query.Domain.Interfaces;
using Ticketing.Query.Infrastructure.Consumers;
using Ticketing.Query.Infrastructure.Data;
using Ticketing.Query.Infrastructure.Repository;

namespace Ticketing.Query.Infrastructure
{
    public static class ConfigurationServices
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
        {
            // Example: services.AddScoped<ICustomerRepository, CustomerRepository>();
            services.AddDbContext<TicketDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("PostgresConnection")));

            //El consumer pide ConsumerConfig directo: se enlaza la sección a una instancia y se registra como singleton
            ConsumerConfig consumerConfig = new ConsumerConfig();
            IConfigurationSection consumerSection = configuration.GetSection("ConsumerConfig");
            consumerSection.Bind(consumerConfig);

            if (string.IsNullOrWhiteSpace(consumerConfig.BootstrapServers)) throw new InvalidOperationException("Falta ConsumerConfig:BootstrapServers.");

            if (string.IsNullOrWhiteSpace(consumerConfig.GroupId)) throw new InvalidOperationException("Falta ConsumerConfig:GroupId.");

            services.AddSingleton(consumerConfig);


            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IEmployeeRepository, EmployeeRepository>();

            services.AddHostedService<ConsumerHostedService>();

            return services;
        }

    }

}


