using MongoDB.Driver;
using Ticketing.Command.Domain.Entities;
using Ticketing.Command.Infrastructure.Interface.Session;

namespace Ticketing.Command.Infrastructure.Interface 
{
    public interface IMongoBaseRepository<TDocument> : ISession
    {
        IQueryable<TDocument> AsQueryable();
        Task InsertOneAsync(TDocument document, IClientSessionHandle clientSessionHandle, CancellationToken cancellationToken);
    }
}

