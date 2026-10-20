using System.Collections;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Ticketing.Command.Infrastructure.Data;

namespace Ticketing.Command.Infrastructure
{
    public static class ConfigureService
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            string? connectionString = configuration.GetConnectionString("MongoDbConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("Falta ConnectionStrings:MongoDbConnection.");
            }

            MongoUrl mongoUrl = MongoUrl.Create(connectionString);
            if (string.IsNullOrWhiteSpace(mongoUrl.DatabaseName))
            {
                throw new InvalidOperationException("La cadena de conexión no incluye el nombre de la base de datos.");
            }

            services.AddSingleton<IMongoClient>(new MongoClient(mongoUrl));
            services.AddSingleton<MongoDbContext>(sp =>
            {
                IMongoClient client = sp.GetRequiredService<IMongoClient>();
                return new MongoDbContext(client, mongoUrl.DatabaseName);
            });                                                //Reintenta automáticamente ante fallos transitorios

            return services;
        }
    }
    
}


