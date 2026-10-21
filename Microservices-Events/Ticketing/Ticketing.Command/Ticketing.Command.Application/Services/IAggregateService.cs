using Ticketing.Command.Domain.Entities.Aggregates;

namespace Ticketing.Command.Application.Services
{
    public interface IAggregateService<TAggregate> where TAggregate : AggregateRoot
    {
        //Se llama tras confirmar la transacción. Devuelve un agregado nuevo, sin cambios pendientes
        TAggregate MarkChangesAsCommitted(TAggregate aggregate);
    }
}
