using MongoDB.Driver;
using Ticketing.Command.Infrastructure.Data;
using Ticketing.Command.Infrastructure.Interface;

namespace Ticketing.Command.Infrastructure.Repository.MongoRepository
{
    public class MongoRepository<TDocument> : IMongoBaseRepository<TDocument>
    {
        private readonly IMongoCollection<TDocument> _mongoCollection;
        private readonly IMongoSessionContext _sessionContext;

        public MongoRepository(MongoDbContext mongoDbContext, IMongoSessionContext sessionContext)
        {
            _mongoCollection = mongoDbContext.GetCollection<TDocument>();
            _sessionContext = sessionContext;
        }

        public IQueryable<TDocument> AsQueryable()
        {
            return _mongoCollection.AsQueryable();
        }

        public async Task InsertOneAsync(TDocument document, CancellationToken cancellationToken)
        {
            IClientSessionHandle? session = _sessionContext.Current;
            if (session is null)
            {
                await _mongoCollection.InsertOneAsync(document, null, cancellationToken);
                return;
            }

            await _mongoCollection.InsertOneAsync(session, document, null, cancellationToken);
        }
    }
}
