using Ticketing.Command.Domain.Entities.EventModels;
using Ticketing.Command.Domain.Interface.Repository;

namespace Ticketing.Command.Domain.Interface.EventModels
{
    public interface IEventModelRepository : IMongoBaseRepository<EventModel>
    {
        
    }
}


