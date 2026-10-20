using MongoDB.Driver;
using Ticketing.Command.Domain.Entities;
using Ticketing.Command.Infrastructure.Interface;

namespace Ticketing.Command.Infrastructure.Repository.MongoDbRepository
{
    public class MongoRepository<TDocument> : IMongoBaseRepository<TDocument>
    {
        public IQueryable<TDocument> AsQueryable()
        {
            throw new NotImplementedException();
        }

        public Task<IClientSessionHandle> BeginSessionAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public void BeginTransaction(IClientSessionHandle clientSessionHandle)
        {
            throw new NotImplementedException();
        }

        public Task CommitTransactionAsync(IClientSessionHandle clientSessionHandle)
        {
            throw new NotImplementedException();
        }

        public void DisposeSession(IClientSessionHandle clientSessionHandle)
        {
            throw new NotImplementedException();
        }

        public Task InsertOneAsync(TDocument document, IClientSessionHandle clientSessionHandle, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task RollbackTransactionAsync(IClientSessionHandle clientSessionHandle, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

    }
}


