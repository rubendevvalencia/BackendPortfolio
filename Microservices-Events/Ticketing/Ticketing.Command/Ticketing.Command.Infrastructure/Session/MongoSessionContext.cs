using MongoDB.Driver;
using Ticketing.Command.Infrastructure.Interface;

namespace Ticketing.Command.Infrastructure.Session
{
    public class MongoSessionContext : IMongoSessionContext
    {
        public IClientSessionHandle? Current { get; set; }
    }
}
