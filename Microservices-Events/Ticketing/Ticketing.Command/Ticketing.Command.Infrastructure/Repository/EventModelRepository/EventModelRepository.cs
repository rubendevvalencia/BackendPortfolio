using MongoDB.Driver;
using Ticketing.Command.Domain.Entities.EventModels;
using Ticketing.Command.Domain.Exceptions;
using Ticketing.Command.Domain.Interfaces.Events;
using Ticketing.Command.Infrastructure.Interface;

namespace Ticketing.Command.Infrastructure.Repository.EventModelRepository
{
    public class EventModelRepository : IEventModelRepository
    {
        private readonly IMongoCollection<EventModel> _collection;
        private readonly IMongoSessionContext _sessionContext;

        public EventModelRepository(IMongoCollection<EventModel> collection, IMongoSessionContext sessionContext)
        {
            _collection = collection;
            _sessionContext = sessionContext;
        }

        public async Task AddAsync(EventModel eventModel, CancellationToken cancellationToken)
        {
            IClientSessionHandle? session = _sessionContext.Current;

            //El índice único (agregado, versión) rechaza un evento con una versión que ya existe: el dominio lo conoce como concurrencia
            try
            {
                if (session is null) await _collection.InsertOneAsync(eventModel, null, cancellationToken);
                else await _collection.InsertOneAsync(session, eventModel, null, cancellationToken);
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                throw new ConcurrencyException(eventModel.AggregateIdentifier, ex);
            }
        }

        public async Task<List<EventModel>> FindByAggregateIdAsync(string aggregateIdentifier, CancellationToken cancellationToken)
        {
            IClientSessionHandle? session = _sessionContext.Current;

            IFindFluent<EventModel, EventModel> query;
            if (session is null) query = _collection.Find(e => e.AggregateIdentifier == aggregateIdentifier);
            else query = _collection.Find(session, e => e.AggregateIdentifier == aggregateIdentifier);

            List<EventModel> events = await query.ToListAsync(cancellationToken);
            return events;
        }
    }
}
