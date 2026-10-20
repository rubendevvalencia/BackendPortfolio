namespace Ticketing.Command.Infrastructure.Interface
{
    public interface IMongoRepository<TDocument>
    {
        IQueryable<TDocument> AsQueryable();
        Task InsertOneAsync(TDocument document, CancellationToken cancellationToken);
    }
}
