using Ecommerce.Transversal.Loggin.Interface;
using Ecommerce.Transversal.Loggin.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Transversal
{
    public static class ConfigureServices
    {
        public static IServiceCollection AddTransversalServices(this IServiceCollection services, IConfiguration configuration)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .Enrich.FromLogContext() //Propiedades del contexto
                .Enrich.WithProperty("Application", "Ecommerce")
                .WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File( //Escribimos un archivo de texto
                    path: "Logs/log-.txt",
                    rollingInterval: RollingInterval.Day, //Por cada día un nuevo archivo
                    retainedFileCountLimit: 7, //Mantener los últimos 7 archivos
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.MSSqlServer( //Te construye la tabla en SQL para que tengas un mejor control sobre lo que está sucediendo con los logs
                    connectionString: configuration.GetConnectionString("EcommerceDb"),
                    sinkOptions: new Serilog.Sinks.MSSqlServer.MSSqlServerSinkOptions
                    {
                        AutoCreateSqlTable = true,
                        TableName = "Logs"
                    },
                    restrictedToMinimumLevel: LogEventLevel.Warning)
                .CreateLogger();

            services.AddSerilog();
            services.AddScoped(typeof(IApiLogger<>), typeof(AppLogger<>));
            return services;
        }
    }
}
