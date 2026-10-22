using Ticketing.Command.Application.Aggregates;
using Ticketing.Common.Events;

namespace Ticketing.Command.Application.Services
{
    public class TicketAggregateService : AggregateService<TicketAggregate>, ITicketAggregateService
    {
        public TicketAggregate Create(string? userName, string? errorType, string? errorDetail)
        {
            string id = Guid.CreateVersion7().ToString();

            TicketCreatedEvent ticketCreatedEvent = new TicketCreatedEvent
            {
                Id = id,
                UserName = userName,
                TyperError = errorType,
                DetailError = errorDetail
            };

            List<EventBase> noChanges = new List<EventBase>();
            TicketAggregate emptyTicket = new TicketAggregate(string.Empty, 0, noChanges, false);
            TicketAggregate ticket = RaiseEvent(emptyTicket, ticketCreatedEvent);

            return ticket;
        }

        protected override TicketAggregate ApplyEvent(TicketAggregate aggregate, EventBase @event)
        {
            switch (@event)
            {
                case TicketCreatedEvent ticketCreatedEvent:
                    TicketAggregate applied = Apply(aggregate, ticketCreatedEvent);
                    return applied;
                default:
                    throw new InvalidOperationException($"El servicio no sabe aplicar el evento {@event.GetType().Name}.");
            }
        }

        protected override TicketAggregate Rebuild(TicketAggregate aggregate, int version, List<EventBase> uncommittedChanges)
        {
            TicketAggregate rebuilt = new TicketAggregate(aggregate.Id, version, uncommittedChanges, aggregate.Active);

            return rebuilt;
        }

        private TicketAggregate Apply(TicketAggregate aggregate, TicketCreatedEvent @event)
        {
            List<EventBase> changes = new List<EventBase>(aggregate.UncommittedChanges);
            TicketAggregate applied = new TicketAggregate(@event.Id, aggregate.Version, changes, true);
            return applied;
        }

        //Reconstruye el estado aplicando los eventos guardados, en orden. Sube la versión pero no los añade a los cambios pendientes
        public TicketAggregate ReplayEvents(TicketAggregate aggregate, IEnumerable<EventBase> events)
        {
            TicketAggregate current = aggregate;

            foreach (EventBase @event in events)
            {
                TicketAggregate applied = ApplyEvent(current, @event);

                int version = current.Version + 1;
                List<EventBase> noChanges = new List<EventBase>();
                current = Rebuild(applied, version, noChanges);
            }

            return current;
        }
    }
}
