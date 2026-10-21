using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Ticketing.Command.Application.Services;

namespace Ticketing.Command.Application
{
    public static class ConfigureServices
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            });

            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
            services.AddScoped(typeof(IEventSourcingHandler<>), typeof(EventSourcingHandler<>));
            services.AddScoped<ITicketAggregateService, TicketAggregateService>();

            return services;
        }
    }
}


