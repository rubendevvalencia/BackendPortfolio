using MediatR;
using Ticketing.Common.Common.Response;
using Ticketing.Query.Domain.Entities;
using Ticketing.Query.Domain.Interfaces;

namespace Ticketing.Query.Application.Feature.Queries.TicketsGet
{
    public class TicketGetCommandHandler : IRequestHandler<TicketGetCommand, Response<string>>
    {
        private readonly IUnitOfWork _unitOfWork;
        public TicketGetCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        //Sin try/catch: si la proyección lanza, la excepción llega al consumer, que no confirma el offset del mensaje
        public async Task<Response<string>> Handle(TicketGetCommand request, CancellationToken cancellationToken)
        {
            bool idIsValid = Guid.TryParse(request.Id, out Guid ticketTypeId);
            if(!idIsValid) return Response<string>.Fail("El Id del evento no es un Guid válido");

            var employee = await _unitOfWork._employeesRepo.GetByUsernameAsync(request.UserName);
            if(employee is null) return Response<string>.NotFound($"No existe el empleado {request.UserName}");

            var ticket = new Ticket()
            {
                Description = request.DetailError,
                TicketTypeId = ticketTypeId,
                TicketType = request.Type
            };

            //GetByUsernameAsync no carga los tickets del empleado: se crea la colección para poder añadir el nuevo
            if(employee.Tickets == null || employee.Tickets.Count == 0) employee.Tickets = new List<Ticket>();
            employee.Tickets.Add(ticket);

            var updated = await _unitOfWork._employeesRepo.UpdateAsync(employee);
            if(!updated) return Response<string>.Fail("Error transacción");

            var savedRows = await _unitOfWork.SaveChangesAsync(cancellationToken);
            if(savedRows <= 0) return Response<string>.Fail("Error transacción");

            return Response<string>.Success(employee.Id.ToString());
        }
    }


}

