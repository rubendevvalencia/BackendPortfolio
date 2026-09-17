using Asp.Versioning;
using Ecommerce.Application.Dto.Jwt;
using Ecommerce.Application.Interface.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Swashbuckle.AspNetCore.Annotations;

namespace Ecommerce.Api.Controllers
{
    [Authorize] //Todos los métodos de la clase necesitan un jwt válido para ser ejecutados, excepto los que tengan [AllowAnonymous].
    [EnableRateLimiting("fixedWindow")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0", Deprecated = true)]
    [ApiVersion("2.0", Deprecated = true)]
    [ApiVersion("3.0")]
    [ApiVersion("4.0")]
    [SwaggerTag("Operaciones relacionadas con la autenticación de usuarios, incluyendo registro y inicio de sesión.")]
    public class UserAuthController : ControllerBase
    {
        private readonly IUserAuthApplication _authApplication;

        public UserAuthController(IUserAuthApplication authApplication)
        {
            _authApplication = authApplication;
        }
        [AllowAnonymous] //Permite el acceso a este método sin necesidad de un jwt válido. Se está registrando para tener token de acceso.
        [HttpPost("SignUp")]
        [SwaggerOperation(Summary = "Registra un nuevo usuario en el sistema.")]
        public async Task<IActionResult> SignUpAsync([FromBody] SignUpDto entity)
        {
            var response = await _authApplication.SignUpAsync(entity);
            if (!response.IsSuccess) return BadRequest(response);
            return Ok(response);
        }
        [AllowAnonymous] //Permite el acceso a este método sin necesidad de un jwt válido. Se está registrando para tener token de acceso.
        [HttpPost("SignIn")]
        [SwaggerOperation(Summary = "Inicia sesión con un usuario existente.")]
        public async Task<IActionResult> SignInAsync([FromBody] SignInDto entity)
        {
            var response = await _authApplication.SingInAsync(entity);
            if (!response.IsSuccess) return Unauthorized(response);
            
            return Ok(response);
        }

        [HttpGet("boom")]
        [AllowAnonymous]   //Importante: el controller lleva [Authorize] a nivel de clase
        public IActionResult Boom() => throw new InvalidOperationException("prueba del middleware");

    }
}
