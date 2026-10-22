using Ticketing.Common.Events;

namespace Ticketing.Command.Domain.Aggregates
{
    public interface IAggregateService<T> where T : AggregateRoot
    {
        //Se llama tras confirmar la transacción. Devuelve un agregado nuevo, sin cambios pendientes
        T MarkChangesAsCommitted(T aggregate);

        //Reconstruye el estado aplicando los eventos guardados, en orden. Sube la versión pero no los añade a los cambios pendientes
        T ReplayEvents(T aggregate, IEnumerable<EventBase> events);

        //Igual que la anterior, pero parte de un agregado vacío que crea el propio servicio. Es la que usa quien carga un agregado por su id
        T ReplayEvents(IEnumerable<EventBase> events);
    }
}
