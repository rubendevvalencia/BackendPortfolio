using AutoMapper;
using MediatR;
using Ticketing.Command.Domain.Entities.EventModels;
using Ticketing.Command.Domain.Enum;
using Ticketing.Command.Domain.Interface.EventModels;
using Ticketing.Command.Domain.Interface.Repository.UnitOfWork;
using Ticketing.Common.Events;
using Ticketing.Common.Response;

namespace Ticketing.Command.Application.Feature.Tickets.TicketsCreate
{
    public class TicketCreateCommandHandler : IRequestHandler<TicketCreateCommand, Response<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IEventModelRepository _eventModelRepo;

        public TicketCreateCommandHandler(IUnitOfWork unitOfWork, IMapper mapper, IEventModelRepository eventModelRepository)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _eventModelRepo = eventModelRepository;
        }
        public async Task<Response<bool>> Handle(TicketCreateCommand request, CancellationToken cancellationToken)
        {
            var response = new Response<bool>();

            var eventModel = new EventModel
            {
                TimeStamp = DateTime.UtcNow,
                AggregateIdentifier = Guid.CreateVersion7(DateTimeOffset.UtcNow).ToString(),
                AggregateType = eAggregateType.TicketAggregate,
                Version = 1,
                EventType = eEventType.TicketCreatedEvent,
                EventData = _mapper.Map<TicketCreatedEvent>(request)
            };

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                await _eventModelRepo.InsertOneAsync(eventModel, cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
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
