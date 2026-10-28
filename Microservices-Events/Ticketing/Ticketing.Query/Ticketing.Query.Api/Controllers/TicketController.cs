using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Swashbuckle.AspNetCore.Annotations;
using Ticketing.Common.Common.Response;
using Ticketing.Query.Application.Feature.Tickets;

namespace Ticketing.Query.Api.Controllers
{
    
    [ApiController]
    //[Authorize] //Protege el controlador completo: cualquier endpoint requiere un token JWT válido.
    //[EnableRateLimiting("user-limited")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    [SwaggerTag("Controller for managing customer operations.")]
    public class TicketController : ApiResponseControllerBase
    {
        private readonly IMediator _mediator;

        public TicketController(IMediator mediator)
        {
            _mediator = mediator;
        }

        //Los endpoints actuales del TicketController (ReadTicket y UpdateTicket) ejecutan comandos de proyección: escriben en el modelo de lectura. 
        //Eso solo debe hacerlo el consumer de Kafka. 
        // El usuario nunca debería poder llamarlos, porque se saltaría el event store y el modelo de lectura dejaría de reflejar los eventos reales.
        //Y no los necesitas: el consumer ya envía esos mismos comandos por MediatR (EventHandler → _mediator.Send), sin pasar por HTTP. 
        // Estos dos endpoints sobran. Si los quieres para probar, déjalos solo en Development o detrás de una política de acceso interno, y quítalos después.
        [HttpGet("ReadTicket")]
        //[RequestTimeout("CustomPolicy")]
        [SwaggerOperation(Summary = "Read a new ticket.", Description = "Read a new ticket to the system.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Ticket readed successfully.", typeof(Response<bool>))]
        public async Task<IActionResult> Create([FromBody] TicketCreatedCommand command, CancellationToken cancellationToken)
        {
            var response = await _mediator.Send(command, cancellationToken);
            return ToActionResult(response);
        }

        [HttpGet("UpdateTicket")]
        //[RequestTimeout("CustomPolicy")]
        [SwaggerOperation(Summary = "Update a ticket.", Description = "Update a new ticket to the system.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Ticket updated successfully.", typeof(Response<bool>))]
        public async Task<IActionResult> Create([FromBody] TicketUpdatedCommand command, CancellationToken cancellationToken)
        {
            var response = await _mediator.Send(command, cancellationToken);
            return ToActionResult(response);
        }
    }
}
