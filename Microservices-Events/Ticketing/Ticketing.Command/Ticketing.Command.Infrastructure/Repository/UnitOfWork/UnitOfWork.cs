using MongoDB.Driver;
using Ticketing.Command.Domain.Interfaces;
using Ticketing.Command.Infrastructure.Interface;

namespace Ticketing.Command.Infrastructure.Repository.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork, IDisposable
    {
        private readonly IMongoClient _mongoClient;
        private readonly ClientSessionOptions _sessionOptions;
        private readonly IMongoSessionContext _sessionContext;

        public UnitOfWork(IMongoClient mongoClient, ClientSessionOptions sessionOptions, IMongoSessionContext sessionContext)
        {
            _mongoClient = mongoClient;
            _sessionOptions = sessionOptions;
            _sessionContext = sessionContext;
        }

        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            IClientSessionHandle clientSession = await _mongoClient.StartSessionAsync(_sessionOptions, cancellationToken);
            clientSession.StartTransaction();
            _sessionContext.Current = clientSession;
        }

        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            IClientSessionHandle? clientSession = _sessionContext.Current;
            if (clientSession is null) throw new InvalidOperationException("No hay una transacción iniciada.");

            await clientSession.CommitTransactionAsync(cancellationToken);
        }

        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            IClientSessionHandle? clientSession = _sessionContext.Current;
            if (clientSession is null) return;
            await clientSession.AbortTransactionAsync(cancellationToken);
        }

        public void Dispose() => CloseSession();

        private void CloseSession()
        {
            IClientSessionHandle? clientSession = _sessionContext.Current;
            if (clientSession is null) return;
            clientSession.Dispose();
            _sessionContext.Current = null;
        }
    }
}
