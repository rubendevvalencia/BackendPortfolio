using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Ticketing.Command.Application.Common;
using Ticketing.Command.Application.Common.Behaviour;
using Ticketing.Command.Application.EventSourcing;
using Ticketing.Command.Application.Features.Tickets.Create;
using Ticketing.Command.Domain.Aggregates;
using Ticketing.Command.Domain.Aggregates.Tickets;
using Ticketing.Command.Domain.Interfaces;
using Ticketing.Command.Domain.Interfaces.Events;
using Ticketing.Command.Domain.Services;

namespace Ticketing.Command.Application
{
    public static class ConfigureServices
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehaviour<,>));
            });

            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            //Interfaces
            services.AddScoped<ITicketAggregateService, TicketAggregateService>();
            services.AddScoped(typeof(IEventSourcingHandler<>), typeof(EventSourcingHandler<>));

            //El handler genérico pide IAggregateService<TicketAggregate>: se resuelve con el mismo servicio del ticket
            services.AddScoped<IAggregateService<TicketAggregate>>(sp => sp.GetRequiredService<ITicketAggregateService>());

            return services;
        }
    }
}


