using Ticketing.Common.Events;

namespace Ticketing.Command.Domain.Abstracts
{
    public interface IEventStore
    {
        Task<List<EventBase>> GetEventsAsync(string aggregateId, CancellationToken cancellationToken);

        Task SaveEventsAsync(string aggregateId, IEnumerable<EventBase> events, int expectedVersion, CancellationToken cancellationToken);

    }
    
}


