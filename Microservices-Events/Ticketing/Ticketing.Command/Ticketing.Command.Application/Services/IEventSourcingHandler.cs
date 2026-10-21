using Ticketing.Command.Domain.Entities.Aggregates;

namespace Ticketing.Command.Application.Services
{
    public interface IEventSourcingHandler<TAggregate> where TAggregate : AggregateRoot
    {
        //Guarda un EventModel por cada cambio pendiente del agregado. La transacción la abre y confirma el caso de uso
        Task SaveAsync(TAggregate aggregate, CancellationToken cancellationToken);
    }
}
