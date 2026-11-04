using Ticketing.Query.Application.Dto;
using Ticketing.Query.Domain.Entities;

namespace Ticketing.Query.Application.Mapping
{
    public static class TicketMapper
    {
        public static List<TicketDto> MappingToDto(IEnumerable<Ticket> tickets)
        {
            List<TicketDto> ticketDtos = new List<TicketDto>();
            foreach(var ticket in tickets)
            {
                ticketDtos.Add(
                    new TicketDto()
                    {
                        Description = ticket.Description,
                        TicketTypeId = ticket.TicketTypeId,
                        TicketType = ticket.TicketType,
                        LastUpdatedAt = ticket.LastUpdatedAt,
                        LastUpdatedBy = ticket.LastUpdatedBy
                    }
                );
            }

            return ticketDtos;
        }
    }

}

