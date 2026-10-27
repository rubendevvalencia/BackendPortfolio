using MediatR;
using Ticketing.Common.Common.Response;

namespace Ticketing.Query.Application.Feature.Tickets
{
    public class TicketCreateCommand : IRequest<Response<bool>>
    {
        public int Id {get; set;}
        public string? UserName {get; set;}
        public int TicketType {get; set;}
        public string? DetailError {get; set;}
    }
    
}


