using Ticketing.Common.Events;

namespace Ticketing.Command.Domain.Aggregates
{
    public abstract class AggregateService<TAggregate> : IAggregateService<TAggregate> where TAggregate : AggregateRoot
    {
        //Se llama tras confirmar la transacción: vacía los cambios pendientes y fija la versión previa a la actual, porque ya están guardados
        public TAggregate MarkChangesAsCommitted(TAggregate aggregate)
        {
            List<EventBase> noChanges = new List<EventBase>();
            TAggregate committed = Rebuild(aggregate, aggregate.Version, aggregate.Version, noChanges);

            return committed;
        }

        //Reconstruye el estado aplicando los eventos ya guardados, en orden. Sube la versión con cada uno pero no los añade a los cambios pendientes
        public TAggregate ReplayEvents(TAggregate aggregate, IEnumerable<EventBase> events)
        {
            TAggregate current = aggregate;

            foreach (EventBase @event in events)
            {
                //Cambia el estado con el evento
                TAggregate applied = ApplyEvent(current, @event);

                //Es un evento ya guardado: sube la versión y la versión previa la iguala, sin cambios pendientes
                int version = current.Version + 1;
                List<EventBase> noChanges = new List<EventBase>();
                current = Rebuild(applied, version, version, noChanges);
            }

            return current;
        }

        //Lanza un evento nuevo: cambia el estado, sube la versión y añade el evento a los cambios pendientes. La versión previa se conserva
        protected TAggregate RaiseEvent(TAggregate aggregate, EventBase @event)
        {
            //Cambia el estado con el evento
            TAggregate applied = ApplyEvent(aggregate, @event);

            //Copia los cambios pendientes que ya había y añade el nuevo
            int version = aggregate.Version + 1;
            List<EventBase> changes = new List<EventBase>(aggregate.UncommittedChanges);
            changes.Add(@event);

            TAggregate raised = Rebuild(applied, version, aggregate.VersionBeforeChanges, changes);

            return raised;
        }

        //Cada servicio decide cómo cambia el estado de su agregado con cada evento. Devuelve un agregado nuevo
        protected abstract TAggregate ApplyEvent(TAggregate aggregate, EventBase @event);

        //Copia del agregado con otra versión, otra versión previa a los cambios y otros cambios pendientes, sin tocar el resto del estado
        protected abstract TAggregate Rebuild(TAggregate aggregate, int version, int versionBeforeChanges, List<EventBase> uncommittedChanges);
    }
}
