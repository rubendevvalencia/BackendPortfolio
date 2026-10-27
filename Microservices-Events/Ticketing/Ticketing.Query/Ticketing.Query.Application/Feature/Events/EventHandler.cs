using MediatR;
using Ticketing.Common.Events;
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
        public Task On(TicketCreatedEvent @event)
        {
            throw new NotImplementedException();
        }
    }


}


