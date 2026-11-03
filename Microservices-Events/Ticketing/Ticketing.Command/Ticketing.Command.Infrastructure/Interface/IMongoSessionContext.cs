using MongoDB.Driver;

namespace Ticketing.Command.Infrastructure.Interface
{
    public interface IMongoSessionContext
    {
        IClientSessionHandle? Current { get; set; }
    }
}
