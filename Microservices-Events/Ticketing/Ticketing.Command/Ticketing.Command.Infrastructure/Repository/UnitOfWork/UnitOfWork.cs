using MongoDB.Driver;
using Ticketing.Command.Domain.Interface.Repository.UnitOfWork;
using Ticketing.Command.Infrastructure.Interface;
using Ticketing.Command.Infrastructure.Interface.Session;

namespace Ticketing.Command.Infrastructure.Repository.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork, IDisposable
    {
        private readonly ISession _session;
        private readonly IMongoSessionContext _sessionContext;

        public UnitOfWork(ISession session, IMongoSessionContext sessionContext)
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

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            IClientSessionHandle? clientSession = _sessionContext.Current;
            if (clientSession is null)
            {
                throw new InvalidOperationException("No hay una transacción iniciada.");
            }

            try
            {
                await _session.CommitTransactionAsync(clientSession, cancellationToken);
            }
            finally
            {
                CloseSession();
            }
        }

        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            IClientSessionHandle? clientSession = _sessionContext.Current;
            if (clientSession is null)
            {
                return;
            }

            try
            {
                await _session.RollbackTransactionAsync(clientSession, cancellationToken);
            }
            finally
            {
                CloseSession();
            }
        }

        public void Dispose()
        {
            CloseSession();
        }

        private void CloseSession()
        {
            IClientSessionHandle? clientSession = _sessionContext.Current;
            if (clientSession is null)
            {
                return;
            }

            _session.DisposeSession(clientSession);
            _sessionContext.Current = null;
        }
    }
}
