using Ecommerce.Transversal.Common;
using MediatR;

namespace Ticketing.Command.Application.Feature.Tickets.TicketsCreate
{
    public class TicketCreateCommand : IRequest<Response<bool>>
    {
        public string UserName {get; set;}
        public string ErrorType {get; set;}
        public string DetailError {get; set;}
    }
}


