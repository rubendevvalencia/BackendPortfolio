using Ticketing.Command.Application.Services;
using Ticketing.Command.Domain.Abstracts;
using Ticketing.Common.Events;

namespace Ticketing.Command.Infrastructure.Persistence
{
    public class EventStore : IEventStore
    {
        public Task<List<EventBase>> GetEventsAsync(string aggregateId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task SaveEventsAsync(string aggregateId, IEnumerable<EventBase> events, int expectedVersion, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

    }
}

