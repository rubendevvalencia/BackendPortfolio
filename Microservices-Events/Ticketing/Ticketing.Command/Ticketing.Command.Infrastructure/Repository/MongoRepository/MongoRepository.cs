using System.Reflection.Metadata.Ecma335;
using MongoDB.Driver;
using Ticketing.Command.Domain.Common;
using Ticketing.Command.Infrastructure.Data;
using Ticketing.Command.Infrastructure.Interface;

namespace Ticketing.Command.Infrastructure.Repository.MongoRepository
{
    public class MongoRepository<TDocument> : IMongoBaseRepository<TDocument>
    {
        private readonly MongoDbContext _mongoDbContext;
        private readonly IMongoCollection<TDocument> _mongoCollection;
       
        public MongoRepository()
        {
            
        }
        public IQueryable<TDocument> AsQueryable()
        {
            throw new NotImplementedException();
        }

        public async Task<IClientSessionHandle> BeginSessionAsync(CancellationToken cancellationToken)
        {
            var option = new ClientSessionOptions();
            option.DefaultTransactionOptions = new TransactionOptions();
            return await _mongoCollection.Database.Client.StartSessionAsync();
        }

        public void BeginTransaction(IClientSessionHandle clientSessionHandle) => clientSessionHandle.StartTransaction();
        public Task CommitTransactionAsync(IClientSessionHandle clientSessionHandle, CancellationToken cancellationToken) => clientSessionHandle.CommitTransactionAsync(cancellationToken);
        public void DisposeSession(IClientSessionHandle clientSessionHandle) => clientSessionHandle.Dispose();
        public async Task InsertOneAsync(TDocument document, IClientSessionHandle clientSessionHandle, CancellationToken cancellationToken)
        {
            await _mongoCollection.InsertOneAsync(clientSessionHandle, document, null, cancellationToken);
        }

        public Task RollbackTransactionAsync(IClientSessionHandle clientSessionHandle, CancellationToken cancellationToken) => clientSessionHandle.AbortTransactionAsync(cancellationToken);
    
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            throw new Exception();
        }
    }
        
}


