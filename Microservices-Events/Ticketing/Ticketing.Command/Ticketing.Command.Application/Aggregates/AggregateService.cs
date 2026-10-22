using Ticketing.Command.Application.Aggregates;
using Ticketing.Common.Events;

namespace Ticketing.Command.Application.Services
{
    public abstract class AggregateService<TAggregate> : IAggregateService<TAggregate> where TAggregate : AggregateRoot
    {
        public TAggregate MarkChangesAsCommitted(TAggregate aggregate)
        {
            List<EventBase> noChanges = new List<EventBase>();
            TAggregate committed = Rebuild(aggregate, aggregate.Version, noChanges);

            return committed;
        }

        protected TAggregate RaiseEvent(TAggregate aggregate, EventBase @event)
        {
            TAggregate applied = ApplyEvent(aggregate, @event);

            int version = aggregate.Version + 1;
            List<EventBase> changes = new List<EventBase>(aggregate.UncommittedChanges);
            changes.Add(@event);

            TAggregate raised = Rebuild(applied, version, changes);

            return raised;
        }

        //Cada servicio decide cómo cambia el estado de su agregado con cada evento. Devuelve un agregado nuevo
        protected abstract TAggregate ApplyEvent(TAggregate aggregate, EventBase @event);

        //Copia del agregado con otra versión y otros cambios pendientes, sin tocar el resto del estado
        protected abstract TAggregate Rebuild(TAggregate aggregate, int version, List<EventBase> uncommittedChanges);
    }
}
