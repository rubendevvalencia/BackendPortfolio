using System.Net;
using Asp.Versioning;
using Ecommerce.Application.Feature.Users.Commands.SignIn;
using Ecommerce.Application.Feature.Users.Commands.SignUp;
using Ecommerce.Transversal.Common;
using Ecommerce.Transversal.Common.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Swashbuckle.AspNetCore.Annotations;

namespace Ecommerce.Api.Controllers.UserAuth.UserAuthControllerCqrs
{
    [Authorize] //Todos los métodos de la clase necesitan un jwt válido para ser ejecutados, excepto los que tengan [AllowAnonymous].
    [EnableRateLimiting("user-limited")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0", Deprecated = true)]
    [ApiVersion("2.0", Deprecated = true)]
    [ApiVersion("3.0", Deprecated = true)]
    [ApiVersion("4.0")]
    public class UserAuthControllerCqrs : ApiResponseControllerBase
    {
        private readonly IMediator _mediator;

        public UserAuthControllerCqrs(IMediator mediator)
        {
            _mediator = mediator;
        }

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

        [AllowAnonymous] //Permite el acceso a este método sin necesidad de un jwt válido. Se está registrando para tener token de acceso.
        [EnableRateLimiting("auth-limited")] //Pisa la politica de la clase: SignUp necesita el limite mas estricto, no el general.
        [HttpPost("SignUp")]
        [SwaggerOperation(Summary = "Registra un nuevo usuario en el sistema.")]
        [SwaggerResponse(StatusCodes.Status200OK, "SignIn successfully.", typeof(Response<bool>))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "The signin data is invalid.", typeof(Response<object>))]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Internal error", typeof(Response<bool>))]
        public async Task<IActionResult> SignUpAsync([FromBody] SignUpCommand request)
        {
            var response = await _mediator.Send(request);
            return ToActionResult(response);
        }

        [AllowAnonymous] //Permite el acceso a este método sin necesidad de un jwt válido. Se está registrando para tener token de acceso.
        [EnableRateLimiting("auth-limited")] //Pisa la politica de la clase: SignIn necesita el limite mas estricto, no el general.
        [HttpPost("SignIn")]
        [SwaggerOperation(Summary = "Inicia sesión con un usuario existente.")]
        [SwaggerResponse(StatusCodes.Status200OK, "SignIn successfully.", typeof(Response<bool>))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "The signin data is invalid.", typeof(Response<object>))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Unauthorized", typeof(Response<bool>))]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Internal error", typeof(Response<bool>))]
        public async Task<IActionResult> SignInAsync([FromBody] SignInCommand request)
        {
            var response = await _mediator.Send(request);
            return ToActionResult(response);
        }
    }
}


