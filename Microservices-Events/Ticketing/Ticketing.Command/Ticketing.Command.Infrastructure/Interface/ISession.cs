using MongoDB.Driver;

namespace Ticketing.Command.Infrastructure.Interface.Session
{
    public interface IMongoSession
    {
        Task<IClientSessionHandle> BeginSessionAsync(CancellationToken cancellationToken);
        void BeginTransaction(IClientSessionHandle clientSessionHandle);
        Task CommitTransactionAsync(IClientSessionHandle clientSessionHandle, CancellationToken cancellationToken);
        Task RollbackTransactionAsync(IClientSessionHandle clientSessionHandle, CancellationToken cancellationToken);
        void DisposeSession(IClientSessionHandle clientSessionHandle);
    }
}
