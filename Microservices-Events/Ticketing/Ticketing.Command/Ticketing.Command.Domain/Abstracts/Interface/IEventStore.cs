using Ticketing.Command.Domain.Enum;
using Ticketing.Common.Events;

namespace Ticketing.Command.Domain.Abstracts
{
    public interface IEventStore
    {
        Task<List<EventBase>> GetEventsAsync(string aggregateId, CancellationToken cancellationToken);

        Task SaveEventsAsync(string aggregateId, eAggregateType aggregateType, IEnumerable<EventBase> events, int expectedVersion, CancellationToken cancellationToken);

    }
    
}


