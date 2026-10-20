using MongoDB.Driver;
using Ticketing.Command.Infrastructure.Interface.Session;

namespace Ticketing.Command.Infrastructure.Session
{
    public class MongoSession : ISession
    {
        private readonly IMongoClient _mongoClient;

        public MongoSession(IMongoClient mongoClient)
        {
            _mongoClient = mongoClient;
        }

        public async Task<IClientSessionHandle> BeginSessionAsync(CancellationToken cancellationToken)
        {
            ClientSessionOptions options = new ClientSessionOptions();
            options.DefaultTransactionOptions = new TransactionOptions();
            IClientSessionHandle session = await _mongoClient.StartSessionAsync(options, cancellationToken);
            return session;
        }

        public void BeginTransaction(IClientSessionHandle clientSessionHandle) => clientSessionHandle.StartTransaction();
        public Task CommitTransactionAsync(IClientSessionHandle clientSessionHandle, CancellationToken cancellationToken) => clientSessionHandle.CommitTransactionAsync(cancellationToken);
        public Task RollbackTransactionAsync(IClientSessionHandle clientSessionHandle, CancellationToken cancellationToken) => clientSessionHandle.AbortTransactionAsync(cancellationToken);
        public void DisposeSession(IClientSessionHandle clientSessionHandle) => clientSessionHandle.Dispose();
    }
}
