using Ticketing.Command.Domain.Aggregates;
using Ticketing.Common.Events;

namespace Ticketing.Command.Domain.Aggregates.Tickets
{
    public class TicketAggregate : AggregateRoot
    {
        public bool Active { get; }

        public TicketAggregate(string id, int version, int versionBeforeChanges, List<EventBase> uncommittedChanges, bool active)
            : base(id, version, versionBeforeChanges, uncommittedChanges)
        {
            Active = active;
        }
    }
}
