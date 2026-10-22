namespace Ticketing.Command.Domain.Interface.Repository.UnitOfWork
{
    public interface IUnitOfWork
    {
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);
        Task RollbackAsync(CancellationToken cancellationToken = default);
        void Dispose();
    }
}
