using Ticketing.Command.Domain.Entities.EventModels;

namespace Ticketing.Command.Domain.Interface.EventModels
{
    public interface IEventModelRepository
    {
        Task InsertOneAsync(EventModel eventModel, CancellationToken cancellationToken);
        Task<List<EventModel>> FindByAggregateIdAsync(string aggregateIdentifier, CancellationToken cancellationToken);
    }
}
