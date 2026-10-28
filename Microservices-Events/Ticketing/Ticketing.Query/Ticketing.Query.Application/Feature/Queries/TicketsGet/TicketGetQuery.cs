using MediatR;
using Ticketing.Common.Common.Response;
using Ticketing.Common.Events;
using Ticketing.Query.Application.Dto;

namespace Ticketing.Query.Application.Feature.Queries.TicketsGet
{
    public class TicketGetQuery : IRequest<Response<List<TicketDto>>>
    {
        public string? Id {get; set;}
        public string? UserName {get; set;}
        public eEventType Type {get; set;}
        public string? DetailError {get; set;}
    }
    
}


