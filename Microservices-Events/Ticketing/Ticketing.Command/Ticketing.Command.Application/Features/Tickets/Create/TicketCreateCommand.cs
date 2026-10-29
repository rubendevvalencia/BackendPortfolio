using MediatR;
using Ticketing.Command.Application.Common.Interface;
using Ticketing.Common.Common.Response;

namespace Ticketing.Command.Application.Features.Tickets.Create
{
    public class TicketCreateCommand : IRequest<Response<string>>, IValidatableRequest //Sin IValidatableRequest, no valida
    {
        public string? UserName {get; set;}
        public string? ErrorType {get; set;}
        public string? ErrorDetail{get; set;}
    }
}


