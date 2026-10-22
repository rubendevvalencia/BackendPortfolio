using MediatR;
using Ticketing.Command.Application.Aggregates;
using Ticketing.Command.Application.Services;
using Ticketing.Command.Domain.Interface.Repository.UnitOfWork;
using Ticketing.Common.Response;

namespace Ticketing.Command.Application.Feature.Tickets.TicketsCreate
{
    public class TicketCreateCommandHandler : IRequestHandler<TicketCreateCommand, Response<bool>>
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
        public async Task<Response<bool>> Handle(TicketCreateCommand request, CancellationToken cancellationToken)
        {
            var response = new Response<bool>();

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

            response.Data = true;
            response.IsSuccess = true;

            return response;
        }
    }

}
