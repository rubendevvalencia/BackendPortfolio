using Ticketing.Common.Events;

namespace Ticketing.Command.Application.Aggregates
{
    public class TicketAggregate : AggregateRoot
    {
        public bool Active { get; }

        public TicketAggregate(string id, int version, List<EventBase> uncommittedChanges, bool active)
            : base(id, version, uncommittedChanges)
        {
            Active = active;
        }
    }
}
