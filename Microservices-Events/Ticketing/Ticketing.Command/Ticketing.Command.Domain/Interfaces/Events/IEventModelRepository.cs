using Ticketing.Command.Domain.Entities.EventModels;

namespace Ticketing.Command.Domain.Interfaces.Events
{
    public interface IEventModelRepository
    {
        Task InsertOneAsync(EventModel eventModel, CancellationToken cancellationToken);
        Task<List<EventModel>> FindByAggregateIdAsync(string aggregateIdentifier, CancellationToken cancellationToken);
    }
}
