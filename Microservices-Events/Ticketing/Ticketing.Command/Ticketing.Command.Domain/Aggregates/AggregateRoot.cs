using Ticketing.Common.Events;

namespace Ticketing.Command.Domain.Aggregates
{
    public abstract class AggregateRoot
    {
        private string _id;
        public string Id {get {return _id;}}
        public int Version { get; }

        //Versión que tenía el agregado en el almacén antes de los cambios pendientes; es la que espera el control de concurrencia
        public int VersionBeforeChanges { get; }
        public IReadOnlyCollection<EventBase> UncommittedChanges { get; }

        public AggregateRoot(string id, int version, int versionBeforeChanges, List<EventBase> uncommittedChanges)
        {
            _id = id;
            Version = version;
            VersionBeforeChanges = versionBeforeChanges;
            UncommittedChanges = uncommittedChanges;
        }
    }
}
