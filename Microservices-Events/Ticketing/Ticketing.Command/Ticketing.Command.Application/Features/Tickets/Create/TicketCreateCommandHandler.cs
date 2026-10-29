using MediatR;
using Ticketing.Command.Domain.Aggregates.Tickets;
using Ticketing.Command.Domain.Interfaces;
using Ticketing.Command.Domain.Interfaces.Events;
using Ticketing.Common.Common.Response;

namespace Ticketing.Command.Application.Features.Tickets.Create
{
    public class TicketCreateCommandHandler : IRequestHandler<TicketCreateCommand, Response<string>>
    {
        private readonly IUnitOfWork _unitOfWork;
        //Lógica del ticket en memoria: decide qué evento se lanza y cómo queda el ticket (Create, MarkChangesAsCommitted).
        //No guarda nada ni toca Mongo o Kafka
        private readonly ITicketAggregateService _ticketAggregateService;
        //Persiste el agregado como eventos: toma sus cambios pendientes y los entrega al almacén (guardarlos en Mongo y publicarlos en Kafka). 
        // No decide qué evento se lanza ni abre la transacción
        private readonly IEventSourcingHandler<TicketAggregate> _eventSourcingHandler;

        public TicketCreateCommandHandler(IUnitOfWork unitOfWork, ITicketAggregateService ticketAggregateService, IEventSourcingHandler<TicketAggregate> eventSourcingHandler)
        {
            _unitOfWork = unitOfWork;
            _ticketAggregateService = ticketAggregateService;
            _eventSourcingHandler = eventSourcingHandler;
        }
        public async Task<Response<string>> Handle(TicketCreateCommand request, CancellationToken cancellationToken)
        {
            TicketAggregate ticket = _ticketAggregateService.Create(request.UserName, request.ErrorType, request.ErrorDetail);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                //Guarda el TicketCreatedEvent pendiente del ticket (versión 1) dentro de la transacción abierta; 
                //el ticket en sí no se guarda, solo su evento
                await _eventSourcingHandler.SaveAsync(ticket, cancellationToken);
                //Confirma la transacción: el evento pasa a ser definitivo y visible para todos en Mongo.
                //Kafka ya se publicó en SaveAsync y no se deshace si esto falla
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                ticket = _ticketAggregateService.MarkChangesAsCommitted(ticket);
            }
            catch
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                throw;
            }
            //Sin _unitOfWork.Dispose(): UnitOfWork es Scoped e IDisposable, el contenedor cierra la sesión de Mongo al acabar la petición
            return Response<string>.Success(ticket.Id);
        }
    }

}
