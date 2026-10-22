using Ticketing.Command.Domain.Aggregates;
using Ticketing.Command.Domain.Aggregates.Tickets;
using Ticketing.Command.Domain.Interfaces;
using Ticketing.Common.Events;

namespace Ticketing.Command.Domain.Services
{
    public class TicketAggregateService : AggregateService<TicketAggregate>, ITicketAggregateService
    {
        //Crea un ticket nuevo: lanza un TicketCreatedEvent sobre un ticket vacío. El resultado queda en versión 1 con ese evento como cambio pendiente
        public TicketAggregate Create(string? userName, string? errorType, string? errorDetail)
        {
            //El id del ticket lo genera el servicio y viaja dentro del evento
            string id = Guid.CreateVersion7().ToString();

            TicketCreatedEvent ticketCreatedEvent = new TicketCreatedEvent
            {
                Id = id,
                UserName = userName,
                TyperError = errorType,
                DetailError = errorDetail
            };

            //Punto de partida: ticket vacío
            TicketAggregate emptyTicket = CreateEmpty();
            TicketAggregate ticket = RaiseEvent(emptyTicket, ticketCreatedEvent);

            return ticket;
        }

        //Ticket sin id, inactivo, en versión 0 y sin cambios pendientes
        protected override TicketAggregate CreateEmpty()
        {
            List<EventBase> noChanges = new List<EventBase>();
            TicketAggregate emptyTicket = new TicketAggregate(string.Empty, 0, 0, noChanges, false);

            return emptyTicket;
        }

        //Elige cómo cambia el ticket según el tipo de evento. Un evento nuevo del ticket añade aquí un case; uno desconocido lanza error
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

        //Devuelve una copia del ticket con la versión, la versión previa y los cambios pendientes indicados. El resto del estado (Id, Active) se copia igual
        protected override TicketAggregate CopyWith(TicketAggregate aggregate, int version, int versionBeforeChanges, List<EventBase> uncommittedChanges)
        {
            TicketAggregate rebuilt = new TicketAggregate(aggregate.Id, version, versionBeforeChanges, uncommittedChanges, aggregate.Active);

            return rebuilt;
        }

        //Cambia el estado del ticket con TicketCreatedEvent: toma el Id del evento y lo marca como activo. La versión y los cambios los fija después Rebuild
        private TicketAggregate Apply(TicketAggregate aggregate, TicketCreatedEvent @event)
        {
            List<EventBase> changes = new List<EventBase>(aggregate.UncommittedChanges);
            TicketAggregate applied = new TicketAggregate(@event.Id, aggregate.Version, aggregate.VersionBeforeChanges, changes, true);
            return applied;
        }
    }
}
