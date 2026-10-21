using MongoDB.Driver;
using Ticketing.Command.Domain.Interface.Repository.UnitOfWork;
using Ticketing.Command.Infrastructure.Interface;
using Ticketing.Command.Infrastructure.Interface.Session;

namespace Ticketing.Command.Infrastructure.Repository.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork, IDisposable
    {
        private readonly IMongoSession _session;
        private readonly IMongoSessionContext _sessionContext;

        public UnitOfWork(IMongoSession session, IMongoSessionContext sessionContext)
        {
            _session = session;
            _sessionContext = sessionContext;
        }

        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            IClientSessionHandle clientSession = await _session.BeginSessionAsync(cancellationToken);
            _session.BeginTransaction(clientSession);
            _sessionContext.Current = clientSession;
        }

        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            IClientSessionHandle? clientSession = _sessionContext.Current;
            if (clientSession is null) throw new InvalidOperationException("No hay una transacción iniciada.");
            
            await _session.CommitTransactionAsync(clientSession, cancellationToken);
        }

        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            IClientSessionHandle? clientSession = _sessionContext.Current;
            if (clientSession is null) return;
            await _session.RollbackTransactionAsync(clientSession, cancellationToken); 
        }

        public void Dispose() => CloseSession();

        private void CloseSession()
        {
            IClientSessionHandle? clientSession = _sessionContext.Current;
            if (clientSession is null) return;
            _session.DisposeSession(clientSession);
            _sessionContext.Current = null;
        }
    }
}
