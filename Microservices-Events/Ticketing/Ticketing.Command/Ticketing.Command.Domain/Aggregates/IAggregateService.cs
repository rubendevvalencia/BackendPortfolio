using Ticketing.Command.Application.Aggregates;

namespace Ticketing.Command.Application.Services
{
    public interface IAggregateService<T> where T : AggregateRoot
    {
        //Se llama tras confirmar la transacción. Devuelve un agregado nuevo, sin cambios pendientes
        T MarkChangesAsCommitted(T aggregate);
    }
}
