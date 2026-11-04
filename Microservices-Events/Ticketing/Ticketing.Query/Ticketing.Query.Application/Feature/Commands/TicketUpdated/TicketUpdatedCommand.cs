using MediatR;
using Ticketing.Common.Common.Response;
using Ticketing.Common.Events;

namespace Ticketing.Query.Application.Feature.Commands.TicketUpdated
{
    public class TicketUpdatedCommand : IRequest<Response<string>>
    {
        public string? Id {get; set;}
        public string? UserName {get; set;}
        public eEventType Type {get; set;}
        public string? DetailError {get; set;}
    }
    
}


