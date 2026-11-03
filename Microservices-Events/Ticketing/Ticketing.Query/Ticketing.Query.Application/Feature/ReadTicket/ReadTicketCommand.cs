using MediatR;
using Ticketing.Common.Common.Response;

namespace Ticketing.Query.Application.Feature.ReadTicket
{
    public class ReadTicketCommand : IRequest<Response<bool>>
    {
    }
    
}


