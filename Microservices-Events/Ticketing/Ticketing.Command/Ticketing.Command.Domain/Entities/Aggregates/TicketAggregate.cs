using Ticketing.Common.Events;

namespace Ticketing.Command.Domain.Entities.Aggregates
{
    public class TicketAggregate : AggregateRoot
    {
        public bool Active { get; }

        public TicketAggregate(string id, int version, IReadOnlyList<EventBase> uncommittedChanges, bool active)
            : base(id, version, uncommittedChanges)
        {
            Active = active;
        }
    }
}
