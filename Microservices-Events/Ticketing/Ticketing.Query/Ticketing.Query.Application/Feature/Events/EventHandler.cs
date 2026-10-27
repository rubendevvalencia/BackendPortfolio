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
            var command = new ProjectionTicketCreatedCommand()
            {
                Id = @event.Id,
                UserName = @event.UserName,
                Type = @event.Type,
                DetailError = @event.DetailError

            };

            await _mediator.Send(command);

        }

        public Task On(TicketUpdatedEvent @event)
        {
            throw new NotImplementedException();
        }

    }


}


