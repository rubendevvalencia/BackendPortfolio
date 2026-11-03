using MediatR;
using Ticketing.Common.Common.Response;

namespace Ticketing.Query.Application.Feature.ReadTicket
{
    public class ReadTicketCommandHandler : IRequestHandler<ReadTicketCommand, Response<bool>>
    {
        public Task<Response<bool>> Handle(ReadTicketCommand request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

    }


}


