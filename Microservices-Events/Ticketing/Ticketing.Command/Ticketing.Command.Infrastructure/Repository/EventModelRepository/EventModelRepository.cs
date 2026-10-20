using MongoDB.Driver;
using Ticketing.Command.Domain.Entities.EventModels;
using Ticketing.Command.Domain.Interface.EventModels;
using Ticketing.Command.Infrastructure.Data;
using Ticketing.Command.Infrastructure.Interface;

namespace Ticketing.Command.Infrastructure.Repository.EventModelRepository
{
    public class EventModelRepository : IEventModelRepository
    {
        private readonly IMongoCollection<EventModel> _collection;
        private readonly IMongoSessionContext _sessionContext;

        public EventModelRepository(MongoDbContext dbContext, IMongoSessionContext sessionContext)
        {
            _collection = dbContext.GetCollection<EventModel>();
            _sessionContext = sessionContext;
        }

        public async Task InsertAsync(EventModel eventModel, CancellationToken cancellationToken)
        {
            IClientSessionHandle? session = _sessionContext.Current;
            if (session is null)
            {
                await _collection.InsertOneAsync(eventModel, null, cancellationToken);
                return;
            }

            await _collection.InsertOneAsync(session, eventModel, null, cancellationToken);
        }

        public async Task<List<EventModel>> FindByAggregateIdAsync(string aggregateIdentifier, CancellationToken cancellationToken)
        {
            FilterDefinition<EventModel> filter = Builders<EventModel>.Filter.Eq(e => e.AggregateIdentifier, aggregateIdentifier);
            IClientSessionHandle? session = _sessionContext.Current;

            IFindFluent<EventModel, EventModel> query;
            if (session is null)
            {
                query = _collection.Find(filter);
            }
            else
            {
                query = _collection.Find(session, filter);
            }

            List<EventModel> events = await query.ToListAsync(cancellationToken);
            return events;
        }
    }
}
