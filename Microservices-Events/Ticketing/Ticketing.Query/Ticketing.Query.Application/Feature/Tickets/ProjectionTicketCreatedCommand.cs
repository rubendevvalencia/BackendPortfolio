using MediatR;
using Ticketing.Common.Common.Response;
using Ticketing.Common.Events;

namespace Ticketing.Query.Application.Feature.Tickets
{
    public class ProjectionTicketCreatedCommand : IRequest<Response<bool>>
    {
        public string? Id {get; set;}
        public string? UserName {get; set;}
        public eEventType Type {get; set;}
        public string? DetailError {get; set;}
    }
    
}


