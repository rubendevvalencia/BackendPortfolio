using Ticketing.Common.Events;

namespace Ticketing.Common.Common.Producers.Interface
{
    public interface IEventProducer
    {
        Task ProduceAsync(string topic, EventBase @event);
    }
}


