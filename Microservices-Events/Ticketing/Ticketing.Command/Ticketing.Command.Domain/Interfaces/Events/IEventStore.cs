using Ticketing.Command.Domain.Enums;
using Ticketing.Common.Events;

namespace Ticketing.Command.Domain.Interfaces.Events
{
    public interface IEventStore
    {
        Task<List<EventBase>> GetEventsAsync(string aggregateId, CancellationToken cancellationToken);

        Task SaveEventsAsync(string aggregateId, eAggregateType aggregateType, IEnumerable<EventBase> events, int expectedVersion, CancellationToken cancellationToken);

    }
    
}


