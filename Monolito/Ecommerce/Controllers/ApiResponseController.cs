using System.Net;
using Ecommerce.Transversal.Common;
using Ecommerce.Transversal.Common.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers
{
    public class ApiResponseControllerBase : ControllerBase
    {
        public IActionResult ToActionResult<T>(Response<T> response)
        {
            if (response.IsSuccess) return Ok(response);

            return response.ErrorType switch
            {
                ErrorType.Validation => BadRequest(response),                                           //400: error de validacion.
                ErrorType.InvalidOperation => BadRequest(response),                                      //400: la operacion no es valida en el contexto actual.
                ErrorType.Unauthorized => Unauthorized(response),                                       //401: credenciales invalidas.
                ErrorType.NotFound => NotFound(response),                                            //404: el recurso no existe.
                ErrorType.Duplicated => Conflict(response),                                          //409: duplicado.
                ErrorType.TimeOut => StatusCode((int)HttpStatusCode.GatewayTimeout, response),       //504: la operacion supero el tiempo limite.
                ErrorType.Unexpected => StatusCode((int)HttpStatusCode.InternalServerError, response),  //500: excepcion no controlada.
                ErrorType.None => StatusCode((int)HttpStatusCode.InternalServerError, response),        //500: respuesta fallida marcada como correcta (estado incoherente).
                _ => StatusCode((int)HttpStatusCode.InternalServerError, response)                   //500: valor no contemplado del enum.
            };
        }
    }
    
}


