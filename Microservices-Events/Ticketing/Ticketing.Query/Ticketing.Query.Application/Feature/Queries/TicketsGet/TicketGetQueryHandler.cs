using MediatR;
using Ticketing.Common.Common.Response;
using Ticketing.Query.Application.Dto;
using Ticketing.Query.Application.Mapping;
using Ticketing.Query.Domain.Entities;
using Ticketing.Query.Domain.Interfaces;

namespace Ticketing.Query.Application.Feature.Queries.TicketsGet
{
    public class TicketGetQueryHandler : IRequestHandler<TicketGetQuery, Response<List<TicketDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        public TicketGetQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Response<List<TicketDto>>> Handle(TicketGetQuery request, CancellationToken cancellationToken)
        {
            bool idIsValid = Guid.TryParse(request.Id, out Guid ticketTypeId);
            if(!idIsValid) return Response<List<TicketDto>>.Fail("El Id del evento no es un Guid válido");

            var tickets = await _unitOfWork._ticketsRepo.GetAllAsync();
            if(tickets is null || !tickets.Any()) return Response<List<TicketDto>>.NotFound("No existen tickets");
    
            return Response<List<TicketDto>>.Success(TicketMapper.MappingToDto(tickets));
        }
    }


}

