using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Ticketing.Command.Domain.Entities.EventModels;
using Ticketing.Command.Domain.Interface.EventModels;
using Ticketing.Command.Infrastructure.Repository.MongoRepository;

namespace Ticketing.Command.Infrastructure.Repository.EventModelRepository
{
    public class EventModelRepository : MongoRepository<EventModel>, IEventModelRepository
    {
        public EventModelRepository(IMongoClient mongoClient, IOptions<MongoClientSettings> options)
        {
            
        }
    }
}


