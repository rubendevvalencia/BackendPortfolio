using Ticketing.Command.Domain.Enums;
using Ticketing.Common.Events;

namespace Ticketing.Command.Domain.Aggregates
{
    public abstract class AggregateRoot //Entidades relacionadas y vo que se tratan como una unidad dentro del modelo del dominio
    {
        private string _id;
        public string Id {get {return _id;}}
        public int Version { get; }
        public int VersionBeforeChanges { get; }     //Versión que tenía el agregado en el almacén antes de los cambios pendientes; es la que espera el control de concurrencia
        public IReadOnlyCollection<EventBase> UncommittedChanges { get; } //Cambios que se realizan en los eventos
        public eAggregateType AggregateType { get; }

        public AggregateRoot(eAggregateType eAggregateType, string id, int version, int versionBeforeChanges, List<EventBase> uncommittedChanges)
        {
            _id = id;
            Version = version;
            VersionBeforeChanges = versionBeforeChanges;
            UncommittedChanges = uncommittedChanges;
            AggregateType = eAggregateType;
        }
    }
}
