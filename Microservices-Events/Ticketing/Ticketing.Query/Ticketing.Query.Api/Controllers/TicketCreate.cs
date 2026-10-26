using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Swashbuckle.AspNetCore.Annotations;
using Ticketing.Common.Common.Response;
using Ticketing.Query.Application.Feature.ReadTicket;

namespace Ticketing.Query.Api.Controllers
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
        [HttpGet("ReadTicket")]
        //[RequestTimeout("CustomPolicy")]
        [SwaggerOperation(Summary = "Read a new ticket.", Description = "Read a new ticket to the system.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Ticket readed successfully.", typeof(Response<bool>))]
        public async Task<IActionResult> Create([FromBody] ReadTicketCommand command, CancellationToken cancellationToken)
        {
            var response = await _mediator.Send(command, cancellationToken);
            return ToActionResult(response);
        }
    }
}
