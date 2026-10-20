using Ecommerce.Transversal.Common;
using MediatR;

namespace Ticketing.Command.Application.Feature.Tickets.TicketsCreate
{
    public class TicketCreateCommandHandler : IRequestHandler<TicketCreateCommand, Response<bool>>
    {
        public Task<Response<bool>> Handle(TicketCreateCommand request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

}


