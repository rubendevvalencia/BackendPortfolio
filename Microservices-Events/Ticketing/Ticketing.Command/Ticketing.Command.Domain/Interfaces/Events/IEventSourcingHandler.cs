using Ticketing.Command.Domain.Aggregates;

namespace Ticketing.Command.Domain.Interfaces.Events
{
    public interface IEventSourcingHandler<T> where T : AggregateRoot
    {
        //Guarda un EventModel por cada cambio pendiente del agregado. La transacción la abre y confirma el caso de uso
        Task SaveAsync(T aggregate, CancellationToken cancellationToken);
        Task<T> GetByIdAsync(string aggregateId, CancellationToken cancellationToken);
    }
}
