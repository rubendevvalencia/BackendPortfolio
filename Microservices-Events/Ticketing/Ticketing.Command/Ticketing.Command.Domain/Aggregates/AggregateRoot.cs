using Ticketing.Common.Events;

namespace Ticketing.Command.Application.Aggregates
{
    public abstract class AggregateRoot
    {
        private string _id;
        public string Id {get {return _id;}}
        public int Version { get; }
        public IReadOnlyCollection<EventBase> UncommittedChanges { get; }

        public AggregateRoot(string id, int version, List<EventBase> uncommittedChanges)
        {
            _id = id;
            Version = version;
            UncommittedChanges = uncommittedChanges;
        }
    }
}
