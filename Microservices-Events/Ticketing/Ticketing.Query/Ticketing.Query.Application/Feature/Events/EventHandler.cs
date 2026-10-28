using MediatR;
using Ticketing.Common.Events;
using Ticketing.Query.Application.Feature.Tickets;
using Ticketing.Query.Domain.Interfaces;

namespace Ticketing.Query.Application.Feature.Events
{
    public class EventHandler : IEventHandler //Handler base que orquesta todos los eventos
    {
        private readonly IMediator _mediator;
        public EventHandler(IMediator mediator)
        {
            _mediator = mediator;
        }
        public async Task On(TicketCreatedEvent @event)
        {
            var command = new TicketCreatedCommand()
            {
                Id = @event.Id,
                UserName = @event.UserName,
                Type = @event.Type,
                DetailError = @event.DetailError

            };

            var response = await _mediator.Send(command);

            //Si la proyección falla se lanza para que el consumer no confirme el offset y el evento no se pierda
            if(!response.IsSuccess) throw new InvalidOperationException($"No se pudo proyectar el evento {nameof(TicketCreatedEvent)} {@event.Id}: {response.Message}");

        }

        public async Task On(TicketUpdatedEvent @event)
        {
            var command = new TicketUpdatedCommand()
            {
                Id = @event.Id,
                UserName = @event.Username,
                Type = @event.Type,
                DetailError = @event.DetailError

            };

            var response = await _mediator.Send(command);

            //Si la proyección falla se lanza para que el consumer no confirme el offset y el evento no se pierda
            if(!response.IsSuccess) throw new InvalidOperationException($"No se pudo proyectar el evento {nameof(TicketCreatedEvent)} {@event.Id}: {response.Message}");

        }

    }


}


