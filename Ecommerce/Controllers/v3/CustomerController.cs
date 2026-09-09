using Asp.Versioning;
using Ecommerce.Application.Dto;
using Ecommerce.Application.Feature.Customers.Commands.CreateCustomerCommand;
using Ecommerce.Application.Interface;
using Ecommerce.Transversal.Common;
using Ecommerce.Transversal.Common.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;

namespace Ecommerce.Api.Controllers.v3
{
    [Authorize] //Protege el controlador completo: cualquier endpoint requiere un token JWT válido. Se puede poner en endpoints individuales si se quiere que algunos sean publicos.
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("3.0")]
    [SwaggerTag("Controller for managing customer operations.")] //Con las annotations de Swagger, podemos añadir una descripción a nivel de controlador para que se muestre en la documentación generada por Swagger.
    public class CustomerController : ControllerBase
    {
        private readonly IMediator _mediator; //Orquesta todo

        public CustomerController(IMediator mediator) => _mediator = mediator;

        //Traduce el resultado de la capa Application al status code HTTP que le corresponde.
        //La Api es la unica que conoce HTTP; la Application solo dice QUE ha fallado, no con que codigo.
        private IActionResult ToActionResult<T>(Response<T> response)
        {
            if (response.IsSuccess) return Ok(response);

            return response.ErrorType switch
            {
                ErrorType.Validation => BadRequest(response),                                        //400: el cliente envio datos incorrectos.
                ErrorType.NotFound => NotFound(response),                                            //404: el recurso no existe.
                _ => StatusCode((int)HttpStatusCode.InternalServerError, response)                   //500: fallo inesperado del servidor.
            };
        }

        [HttpPost("AddAsync")]
        [SwaggerOperation(Summary = "Adds a new customer.", Description = "Adds a new customer to the system.")]    //Compensa sobretodo en API de terceros que se generen para su consumo
        [SwaggerResponse(StatusCodes.Status200OK, "Customer added successfully.", typeof(Response<bool>))]          //Compensa sobretodo en API de terceros que se generen para su consumo
        [SwaggerResponse(StatusCodes.Status400BadRequest, "The customer data is invalid.", typeof(Response<bool>))]
        public async Task<IActionResult> AddAsync([FromBody] CreateCustomerCommand command)
        {
            if (command == null) return BadRequest();
           
            var response = await _mediator.Send(command);
            return ToActionResult(response);
        }
    }
}
