using AutoMapper;
using Ecommerce.Transversal.Common;
using MediatR;
using Ticketing.Command.Domain.Interface.Repository.UnitOfWork;

namespace Ticketing.Command.Application.Feature.Tickets.TicketsCreate
{
    public class TicketCreateCommandHandler : IRequestHandler<TicketCreateCommand, Response<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public TicketCreateCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }
        public Task<Response<bool>> Handle(TicketCreateCommand request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

}


