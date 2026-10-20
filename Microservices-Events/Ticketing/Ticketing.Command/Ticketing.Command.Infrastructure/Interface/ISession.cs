using MongoDB.Driver;

namespace Ticketing.Command.Infrastructure.Interface.Session
{
    public interface ISession
    {
        Task<IClientSessionHandle> BeginSessionAsync(CancellationToken cancellationToken);
        void BeginTransaction(IClientSessionHandle clientSessionHandle);
        Task CommitTransactionAsync(IClientSessionHandle clientSessionHandle);
        Task RollbackTransactionAsync(IClientSessionHandle clientSessionHandle, CancellationToken cancellationToken);
        void DisposeSession(IClientSessionHandle clientSessionHandle);
    }
}


