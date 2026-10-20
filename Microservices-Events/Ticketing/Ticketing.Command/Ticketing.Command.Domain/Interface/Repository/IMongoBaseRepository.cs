using MongoDB.Driver;
using Ticketing.Command.Domain.Entities;
using Ticketing.Command.Domain.Interface.Session;

namespace Ticketing.Command.Domain.Interface.Repository
{
    public interface IMongoBaseRepository<TDocument> : ISession where TDocument : DocumentBase
    {
        IQueryable<TDocument> AsQueryable();
        Task InsertOneAsync(TDocument document, IClientSessionHandle clientSessionHandle, CancellationToken cancellationToken);
    }
}


