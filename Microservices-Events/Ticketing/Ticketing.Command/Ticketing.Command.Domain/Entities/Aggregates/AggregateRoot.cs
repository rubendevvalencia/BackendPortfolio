using Ticketing.Common.Events;

namespace Ticketing.Command.Domain.Entities.Aggregates
{
    public abstract class AggregateRoot
    {
        public string Id { get; }
        public int Version { get; }
        public IReadOnlyList<EventBase> UncommittedChanges { get; }

        protected AggregateRoot(string id, int version, IReadOnlyList<EventBase> uncommittedChanges)
        {
            Id = id;
            Version = version;
            UncommittedChanges = uncommittedChanges;
        }
    }
}
