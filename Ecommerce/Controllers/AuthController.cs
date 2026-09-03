using Ecommerce.Application.Dto.Jwt;
using Ecommerce.Application.Interface.Jwt;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Ecommerce.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [SwaggerTag("Operaciones relacionadas con la autenticación de usuarios, incluyendo registro y inicio de sesión.")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthApplication _authApplication;

        public AuthController(IAuthApplication authApplication)
        {
            _authApplication = authApplication;
        }

        [HttpPost("SignUp")]
        [SwaggerOperation(Summary = "Registra un nuevo usuario en el sistema.")]
        public async Task<IActionResult> SignUpAsync([FromBody] SignUpDto entity)
        {
            var response = await _authApplication.SignUpAsync(entity);
            if (!response.IsSuccess) return BadRequest(response);
            return Ok(response);
        }

        [HttpPost("SignIn")]
        [SwaggerOperation(Summary = "Inicia sesión con un usuario existente.")]
        public async Task<IActionResult> SignInAsync([FromBody] SignInDto entity)
        {
            var response = await _authApplication.SingInAsync(entity);
            if (!response.IsSuccess) return Unauthorized(response);
            
            return Ok(response);
        }
    }
}
