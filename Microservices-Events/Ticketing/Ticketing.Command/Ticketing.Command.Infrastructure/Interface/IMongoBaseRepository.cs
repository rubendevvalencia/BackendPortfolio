namespace Ticketing.Command.Infrastructure.Interface
{
    public interface IMongoBaseRepository<TDocument>
    {
        IQueryable<TDocument> AsQueryable();
        Task InsertOneAsync(TDocument document, CancellationToken cancellationToken);
     
    }
}
