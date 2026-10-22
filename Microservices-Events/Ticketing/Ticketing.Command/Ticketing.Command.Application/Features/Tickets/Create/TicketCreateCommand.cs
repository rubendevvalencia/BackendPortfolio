using MediatR;
using Ticketing.Common.Response;

namespace Ticketing.Command.Application.Features.Tickets.Create
{
    public class TicketCreateCommand : IRequest<Response<string>>
    {
        public string? UserName {get; set;}
        public string? ErrorType {get; set;}
        public string? ErrorDetail{get; set;}
    }
}


