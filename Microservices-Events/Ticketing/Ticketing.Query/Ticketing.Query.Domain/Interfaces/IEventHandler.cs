using Ticketing.Common.Events;

namespace Ticketing.Query.Domain.Interfaces
{
    public interface IEventHandler
    {
        Task On(TicketCreatedEvent @event);
    }

    
}

