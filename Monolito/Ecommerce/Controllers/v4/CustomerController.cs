using Asp.Versioning;
using Ecommerce.Application.Dto;
using Ecommerce.Application.Feature.Customers.v4.Commands.CreateCustomer;
using Ecommerce.Application.Feature.Customers.v4.Commands.DeleteCustomer;
using Ecommerce.Application.Feature.Customers.v4.Commands.UpdateCustomer;
using Ecommerce.Application.Feature.Customers.v4.Queries.GetAllCustomerQuery;
using Ecommerce.Application.Feature.Customers.v4.Queries.GetCustomerQuery;
using Ecommerce.Transversal.Common;
using Ecommerce.Transversal.Common.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;

namespace Ecommerce.Api.Controllers.v4
{
    //v4 = v3 con la validacion en el pipeline de MediatR (ValidationBehaviour).
    //El controller ya no comprueba ids ni datos: delega todo y, si la peticion no es valida,
    //ValidationBehaviour lanza ValidationExceptionCustom y GlobalExceptionHandler responde el 400.
    [Authorize] //Protege el controlador completo: cualquier endpoint requiere un token JWT válido.
    [EnableRateLimiting("user-limited")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("4.0")]
    [SwaggerTag("Controller for managing customer operations.")]
    public class CustomerController : ControllerBase
    {
        private readonly IMediator _mediator; //Orquesta todo

        public CustomerController(IMediator mediator)
        {
            _mediator = mediator;
        }

        //Traduce el resultado de la capa Application al status code HTTP que le corresponde.
        //Sin rama de Validation: en v4 ese fallo no viaja en Response, sale como excepcion y lo responde el middleware.
        private IActionResult ToActionResult<T>(Response<T> response)
        {
            if (response.IsSuccess) return Ok(response);

            return response.ErrorType switch
            {
                ErrorType.NotFound => NotFound(response),                                            //404: el recurso no existe.
                ErrorType.Duplicated => Conflict(response),                                          //409: duplicado.
                ErrorType.TimeOut => StatusCode((int)HttpStatusCode.GatewayTimeout, response),       //504: la operacion supero el tiempo limite.
                _ => StatusCode((int)HttpStatusCode.InternalServerError, response)                   //500: fallo inesperado del servidor.
            };
        }

        [HttpPost("Create")]
        [RequestTimeout("CustomPolicy")]
        [SwaggerOperation(Summary = "Adds a new customer.", Description = "Adds a new customer to the system.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Customer added successfully.", typeof(Response<bool>))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "The customer data is invalid.", typeof(Response<object>))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "Customer is already registered", typeof(Response<bool>))]
        public async Task<IActionResult> Create([FromBody] CreateCustomerCommand command, CancellationToken cancellationToken)
        {
            var response = await _mediator.Send(command, cancellationToken);
            return ToActionResult(response);
        }

        [HttpPost("UpdateAsyncPost")]
        [RequestTimeout("CustomPolicy")]
        [SwaggerOperation(Summary = "Updates an existing customer using POST.", Description = "Updates the details of an existing customer in the system using a POST request.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Customer updated successfully.", typeof(Response<bool>))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "The customer data is invalid.", typeof(Response<object>))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "The customer does not exist.", typeof(Response<bool>))]
        public async Task<IActionResult> UpdateAsyncPost([FromBody] UpdateCustomerCommand command, CancellationToken cancellationToken)
        {
            var response = await _mediator.Send(command, cancellationToken);
            return ToActionResult(response);
        }

        [HttpDelete("DeleteAsync/{id}")]
        [RequestTimeout("CustomPolicy")]
        [SwaggerOperation(Summary = "Deletes an existing customer.", Description = "Deletes an existing customer from the system.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Customer deleted successfully.", typeof(Response<bool>))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "The id is invalid.", typeof(Response<object>))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "The customer does not exist.", typeof(Response<bool>))]
        public async Task<IActionResult> DeleteAsync([FromRoute] int id, CancellationToken cancellationToken)
        {
            DeleteCustomerCommand command = new()
            {
                Id = id
            };
            var response = await _mediator.Send(command, cancellationToken);
            return ToActionResult(response);
        }

        [HttpGet("GetByIdAsync/{id}")]
        [RequestTimeout("CustomPolicy")]
        [SwaggerOperation(Summary = "Retrieves a customer by ID.", Description = "Retrieves the details of a customer based on the provided ID.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Customer retrieved successfully.", typeof(Response<CustomerDto>))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "The id is invalid.", typeof(Response<object>))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "The customer does not exist.", typeof(Response<CustomerDto>))]
        public async Task<IActionResult> GetByIdAsync([FromRoute] int id, CancellationToken cancellationToken)
        {
            GetCustomerQuery query = new()
            {
                Id = id
            };
            var response = await _mediator.Send(query, cancellationToken);
            return ToActionResult(response);
        }

        [HttpGet("GetAllAsync")]
        [SwaggerOperation(
            Summary = "Retrieves all customers.", 
            Description = "Retrieves a list of all customers in the system.",
            OperationId = "GetAll",
            Tags = new string[] { "GetAll" })]
        [SwaggerResponse(StatusCodes.Status200OK, "Customers retrieved successfully.", typeof(Response<IEnumerable<CustomerDto>>))]
        public async Task<IActionResult> GetAllAsync(CancellationToken cancellationToken)
        {
            GetAllCustomerQuery query = new GetAllCustomerQuery();
            var response = await _mediator.Send(query, cancellationToken); //TimeOut para cancelación
            return ToActionResult(response);
        }

        //Endpoint de diagnostico: espera mas tiempo que "CustomPolicy" (2000ms) para forzar el corte de
        //RequestTimeoutsMiddleware y poder ver en el front el 504 con Response<T>/ErrorType.TimeOut de verdad.
        [HttpGet("TestTimeout")]
        [RequestTimeout("CustomPolicy")]
        [SwaggerOperation(Summary = "Forces a timeout.", Description = "Diagnostic endpoint: always exceeds CustomPolicy so RequestTimeoutsMiddleware cuts the request.")]
        [SwaggerResponse(StatusCodes.Status504GatewayTimeout, "The request exceeded the time limit.", typeof(Response<object>))]
        public async Task<IActionResult> TestTimeout(CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            return Ok(); //No se llega aqui: el middleware corta la peticion antes de los 5 segundos.
        }
    }
}
