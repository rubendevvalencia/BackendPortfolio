using Ecommerce.Application.Dto;
using Ecommerce.Application.Interface;
using Ecommerce.Transversal.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;
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

        [HttpPost("AddAsync")]
        [SwaggerOperation(Summary = "Adds a new customer.", Description = "Adds a new customer to the system.")]    //Compensa sobretodo en API de terceros que se generen para su consumo
        [SwaggerResponse(StatusCodes.Status200OK, "Customer added successfully.", typeof(Response<bool>))]          //Compensa sobretodo en API de terceros que se generen para su consumo
        public async Task<IActionResult> AddAsync([FromBody] CustomerDto customerDto)
        {
            if (customerDto == null) return BadRequest();

            var response = await _customerApplication.AddAsync(customerDto);
            if(response.IsSuccess) return Ok(response);
            return StatusCode((int)HttpStatusCode.InternalServerError, response);
        }

        [HttpPut("UpdateAsync/{id}")]
        [SwaggerOperation(Summary = "Updates an existing customer.", Description = "Updates the details of an existing customer in the system.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Customer updated successfully.", typeof(Response<bool>))]
        public async Task<IActionResult> UpdateAsync([FromRoute] int id, [FromBody] CustomerDto customerDto)
        {
            if (customerDto == null) return BadRequest();

            if(!id.Equals(customerDto.Id)) return BadRequest(); //Podemos comprobar de manera efectiva que el id de la ruta y el id del objeto sean iguales, si no lo son, devolvemos un BadRequest con un mensaje de error.

            var response = await _customerApplication.UpdateAsync(customerDto);
            if (response.IsSuccess) return Ok(response);
            return StatusCode((int)HttpStatusCode.InternalServerError, response);
        }

        [HttpPost("UpdateAsyncPost/{id}")]
        [SwaggerOperation(Summary = "Updates an existing customer using POST.", Description = "Updates the details of an existing customer in the system using a POST request.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Customer updated successfully.", typeof(Response<bool>))]
        public async Task<IActionResult> UpdateAsyncPost([FromRoute] int id, [FromBody] CustomerDto customerDto)
        {
            if (customerDto == null) return BadRequest();

            if (!id.Equals(customerDto.Id)) return BadRequest(); //Podemos comprobar de manera efectiva que el id de la ruta y el id del objeto sean iguales, si no lo son, devolvemos un BadRequest con un mensaje de error.

            var response = await _customerApplication.UpdateAsync(customerDto);
            if (response.IsSuccess) return Ok(response);
            return StatusCode((int)HttpStatusCode.InternalServerError, response);
        }

        [HttpDelete("DeleteAsync/{id}")]
        [SwaggerOperation(Summary = "Deletes an existing customer.", Description = "Deletes an existing customer from the system.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Customer deleted successfully.", typeof(Response<bool>))]
        public async Task<IActionResult> DeleteAsync([FromRoute] int id)
        {
            if(id<0) return BadRequest(); //Se puede añadir un data annotation para validar que el id sea mayor que 0, pero en este caso lo hacemos de manera manual.

            var response = await _customerApplication.DeleteAsync(id);
            if (response.IsSuccess) return Ok(response);
            return StatusCode((int)HttpStatusCode.InternalServerError, response);
        }

        [HttpGet("GetByIdAsync/{id}")]
        [SwaggerOperation(Summary = "Retrieves a customer by ID.", Description = "Retrieves the details of a customer based on the provided ID.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Customer retrieved successfully.", typeof(Response<CustomerDto>))]
        public async Task<IActionResult> GetByIdAsync([FromRoute] int id)
        {
            if(id<0) return BadRequest("Invalid Id");
            var response = await _customerApplication.GetByIdAsync(id);
            if (response.IsSuccess) return Ok(response);
            return StatusCode((int)HttpStatusCode.InternalServerError, response);
        }

        [HttpGet("GetAllAsync")]
        [SwaggerOperation(Summary = "Retrieves all customers.", Description = "Retrieves a list of all customers in the system.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Customers retrieved successfully.", typeof(Response<IEnumerable<CustomerDto>>))]
        public async Task<IActionResult> GetAllAsync()
        {
            var response = await _customerApplication.GetAllAsync();
            if (response.IsSuccess) return Ok(response);
            return StatusCode((int)HttpStatusCode.InternalServerError, response);
        }
    }
}
