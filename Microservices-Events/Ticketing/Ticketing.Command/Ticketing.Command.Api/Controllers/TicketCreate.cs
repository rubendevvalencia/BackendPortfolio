using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using Ticketing.Command.Application.Features.Tickets.Create;
using MediatR;
using Swashbuckle.AspNetCore.Annotations;
using Ticketing.Common.Common.Response;

namespace Ticketing.Command.Api.Controllers
{
    
    [ApiController]
    //[Authorize] //Protege el controlador completo: cualquier endpoint requiere un token JWT válido.
    //[EnableRateLimiting("user-limited")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    [SwaggerTag("Controller for managing customer operations.")]
    public class TicketCreate : ApiResponseControllerBase
    {
        private readonly IMediator _mediator;

        public TicketCreate(IMediator mediator)
        {
            _mediator = mediator;
        }
        [HttpPost("CreateTicket")]
        //[RequestTimeout("CustomPolicy")]
        [SwaggerOperation(Summary = "Adds a new ticket.", Description = "Adds a new ticket to the system.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Ticket created successfully.", typeof(Response<string>))]
        public async Task<IActionResult> Create([FromBody] TicketCreateCommand command, CancellationToken cancellationToken)
        {
            var response = await _mediator.Send(command, cancellationToken);
            return ToActionResult(response);
        }
    }
}
