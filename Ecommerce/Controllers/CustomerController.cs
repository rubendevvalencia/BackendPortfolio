using Ecommerce.Application.Dto;
using Ecommerce.Application.Interface;
using Ecommerce.Transversal.Common;
using Ecommerce.Transversal.Common.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;

namespace Ecommerce.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [SwaggerTag("Controller for managing customer operations.")] //Con las annotations de Swagger, podemos añadir una descripción a nivel de controlador para que se muestre en la documentación generada por Swagger.
    public class CustomerController : ControllerBase
    {
        private readonly ICustomerApplication _customerApplication;

        public CustomerController(ICustomerApplication customerApplication) =>  _customerApplication = customerApplication;

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
        public async Task<IActionResult> AddAsync([FromBody] CustomerDto customerDto, CancellationToken cancellationToken)
        {
            if (customerDto == null) return BadRequest();
            //El CancellationToken lo inyecta ASP.NET Core (HttpContext.RequestAborted): si el cliente
            //aborta la peticion, la validacion y las consultas se cancelan en lugar de seguir trabajando.
            var response = await _customerApplication.AddAsync(customerDto, cancellationToken);
            return ToActionResult(response);
        }

        [HttpPut("UpdateAsync{id}")]
        [SwaggerOperation(Summary = "Updates an existing customer.", Description = "Updates the details of an existing customer in the system.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Customer updated successfully.", typeof(Response<bool>))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "The customer data is invalid.", typeof(Response<bool>))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "The customer does not exist.", typeof(Response<bool>))]
        public async Task<IActionResult> UpdateAsync([FromRoute] int id, [FromBody] CustomerDto customerDto, CancellationToken cancellationToken)
        {
            if (customerDto == null) return BadRequest();

            var response = await _customerApplication.UpdateAsync(id, customerDto, cancellationToken);
            return ToActionResult(response);
        }

        [HttpPost("UpdateAsyncPost/{id}")]
        [SwaggerOperation(Summary = "Updates an existing customer using POST.", Description = "Updates the details of an existing customer in the system using a POST request.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Customer updated successfully.", typeof(Response<bool>))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "The customer data is invalid.", typeof(Response<bool>))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "The customer does not exist.", typeof(Response<bool>))]
        public async Task<IActionResult> UpdateAsyncPost([FromRoute] int id, [FromBody] CustomerDto customerDto, CancellationToken cancellationToken)
        {
            if (customerDto == null) return BadRequest();

            var response = await _customerApplication.UpdateAsync(id, customerDto, cancellationToken);
            return ToActionResult(response);
        }

        [HttpDelete("DeleteAsync/{id}")]
        [SwaggerOperation(Summary = "Deletes an existing customer.", Description = "Deletes an existing customer from the system.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Customer deleted successfully.", typeof(Response<bool>))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "The customer does not exist.", typeof(Response<bool>))]
        public async Task<IActionResult> DeleteAsync([FromRoute] int id)
        {
            if(id<0) return BadRequest(); //Se puede añadir un data annotation para validar que el id sea mayor que 0, pero en este caso lo hacemos de manera manual.

            var response = await _customerApplication.DeleteAsync(id);
            return ToActionResult(response);
        }

        [HttpGet("GetByIdAsync/{id}")]
        [SwaggerOperation(Summary = "Retrieves a customer by ID.", Description = "Retrieves the details of a customer based on the provided ID.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Customer retrieved successfully.", typeof(Response<CustomerDto>))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "The customer does not exist.", typeof(Response<CustomerDto>))]
        public async Task<IActionResult> GetByIdAsync([FromRoute] int id)
        {
            if(id<0) return BadRequest("Invalid Id");
            var response = await _customerApplication.GetByIdAsync(id);
            return ToActionResult(response);
        }

        [HttpGet("GetAllAsync")]
        [SwaggerOperation(Summary = "Retrieves all customers.", Description = "Retrieves a list of all customers in the system.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Customers retrieved successfully.", typeof(Response<IEnumerable<CustomerDto>>))]
        public async Task<IActionResult> GetAllAsync()
        {
            var response = await _customerApplication.GetAllAsync();
            return ToActionResult(response);
        }
    }
}
