using MediatR;
using Ticketing.Common.Common.Response;
using Ticketing.Query.Domain.Entities;
using Ticketing.Query.Domain.Interfaces;

namespace Ticketing.Query.Application.Feature.Tickets
{
    public class ProjectionTicketCreatedCommandHandler : IRequestHandler<ProjectionTicketCreatedCommand, Response<string>>
    {
        private readonly IUnitOfWork _unitOfWork;
        public ProjectionTicketCreatedCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<Response<string>> Handle(ProjectionTicketCreatedCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var employee = await _unitOfWork._employeesRepo.GetByUsernameAsync(request.UserName);
                var ticket = new Ticket()
                {
                    Description = request.DetailError,
                    TicketTypeId = new Guid(request.Id),
                    TicketType = request.Type
                };

                employee.Tickets.Add(ticket);

                var result = await _unitOfWork._employeesRepo.UpdateAsync(employee);
                if(!result) Response<string>.Fail("Error transacción");
                return Response<string>.Success(nameof(employee.Id));
            }
            catch(Exception)
            {
                return Response<string>.Fail("Excepción");
            }
        }
    }


}

