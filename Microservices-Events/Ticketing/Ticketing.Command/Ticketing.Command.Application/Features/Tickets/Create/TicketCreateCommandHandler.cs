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
        private readonly ITicketAggregateService _ticketAggregateService;
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
                await _eventSourcingHandler.SaveAsync(ticket, cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                ticket = _ticketAggregateService.MarkChangesAsCommitted(ticket);
                _unitOfWork.Dispose();
            }
            catch
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                _unitOfWork.Dispose();
                throw;
            }
            return Response<string>.Success(ticket.Id);
        }
    }

}
