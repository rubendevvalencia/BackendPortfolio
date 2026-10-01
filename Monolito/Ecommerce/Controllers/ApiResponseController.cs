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
                ErrorType.NotFound => NotFound(response),                                            //404: el recurso no existe.
                ErrorType.Duplicated => Conflict(response),                                          //409: duplicado.
                ErrorType.TimeOut => StatusCode((int)HttpStatusCode.GatewayTimeout, response),       //504: la operacion supero el tiempo limite.
                _ => StatusCode((int)HttpStatusCode.InternalServerError, response)                   //500: fallo inesperado del servidor.
            };
        }
    }
    
}


