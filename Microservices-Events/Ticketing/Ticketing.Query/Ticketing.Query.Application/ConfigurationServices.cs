using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Ticketing.Query.Domain.Interfaces;

namespace Ticketing.Query.Application
{
    public static class ConfigurationServices
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            });

            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            //Nombre completo: EventHandler choca con System.EventHandler (using implícito)
            services.AddScoped<IEventHandler, Feature.Events.EventHandler>();

            return services;
        }
    }

}


