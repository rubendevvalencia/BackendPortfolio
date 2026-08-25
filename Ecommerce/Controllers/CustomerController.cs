using Ecommerce.Application.Dto;
using Ecommerce.Application.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace Ecommerce.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerController : ControllerBase
    {
        private readonly ICustomerApplication _customerApplication;

        public CustomerController(ICustomerApplication customerApplication) =>  _customerApplication = customerApplication;

        [HttpPost("AddAsync")]
        public async Task<IActionResult> AddAsync([FromBody] CustomerDto customerDto)
        {
            if (customerDto == null) return BadRequest();

            var response = await _customerApplication.AddAsync(customerDto);
            if(response.IsSuccess) return Ok(response);
            return StatusCode((int)HttpStatusCode.InternalServerError, response);
        }

        [HttpPut("UpdateAsync/{id}")]
        public async Task<IActionResult> UpdateAsync([FromRoute] int id, [FromBody] CustomerDto customerDto)
        {
            if (customerDto == null) return BadRequest();

            if(!id.Equals(customerDto.Id)) return BadRequest(); //Podemos comprobar de manera efectiva que el id de la ruta y el id del objeto sean iguales, si no lo son, devolvemos un BadRequest con un mensaje de error.

            var response = await _customerApplication.UpdateAsync(customerDto);
            if (response.IsSuccess) return Ok(response);
            return StatusCode((int)HttpStatusCode.InternalServerError, response);
        }

        [HttpPost("UpdateAsyncPost/{id}")]
        public async Task<IActionResult> UpdateAsyncPost([FromRoute] int id, [FromBody] CustomerDto customerDto)
        {
            if (customerDto == null) return BadRequest();

            if (!id.Equals(customerDto.Id)) return BadRequest(); //Podemos comprobar de manera efectiva que el id de la ruta y el id del objeto sean iguales, si no lo son, devolvemos un BadRequest con un mensaje de error.

            var response = await _customerApplication.UpdateAsync(customerDto);
            if (response.IsSuccess) return Ok(response);
            return StatusCode((int)HttpStatusCode.InternalServerError, response);
        }

        [HttpDelete("DeleteAsync/{id}")]
        public async Task<IActionResult> DeleteAsync([FromRoute] int id)
        {
            if(id<0) return BadRequest(); //Se puede añadir un data annotation para validar que el id sea mayor que 0, pero en este caso lo hacemos de manera manual.

            var response = await _customerApplication.DeleteAsync(id);
            if (response.IsSuccess) return Ok(response);
            return StatusCode((int)HttpStatusCode.InternalServerError, response);
        }

        [HttpGet("GetByIdAsync/{id}")]
        public async Task<IActionResult> GetByIdAsync([FromRoute] int id)
        {
            if(id<0) return BadRequest("Invalid Id");
            var response = await _customerApplication.GetByIdAsync(id);
            if (response.IsSuccess) return Ok(response);
            return StatusCode((int)HttpStatusCode.InternalServerError, response);
        }

        [HttpGet("GetAllAsync")]
        public async Task<IActionResult> GetAllAsync()
        {
            var response = await _customerApplication.GetAllAsync();
            if (response.IsSuccess) return Ok(response);
            return StatusCode((int)HttpStatusCode.InternalServerError, response);
        }
    }
}
