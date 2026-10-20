using MongoDB.Driver;
using Ticketing.Command.Domain.Interface.Repository.UnitOfWork;

namespace Ticketing.Command.Infrastructure.Interface.Session
{
    public interface ISession : IUnitOfWork
    {
        Task<IClientSessionHandle> BeginSessionAsync(CancellationToken cancellationToken);
        void BeginTransaction(IClientSessionHandle clientSessionHandle);
        Task CommitTransactionAsync(IClientSessionHandle clientSessionHandle, CancellationToken cancellationToken);
        Task RollbackTransactionAsync(IClientSessionHandle clientSessionHandle, CancellationToken cancellationToken);
        void DisposeSession(IClientSessionHandle clientSessionHandle);
    }
}


