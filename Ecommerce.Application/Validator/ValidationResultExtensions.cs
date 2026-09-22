using System.Linq;
using Ecommerce.Transversal.Common;
using FluentValidation.Results;

namespace Ecommerce.Application.Validator
{
    //Punto unico de traduccion entre el resultado de FluentValidation y el Response<T> que
    //devuelve la capa Application. Evita repetir el mismo bloque en cada caso de uso.
    public static class ValidationResultExtensions
    {
        public static Response<T> ToFailedResponse<T>(this ValidationResult validationResult)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)                                   //Agrupa por propiedad: "City", "Phone"...
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToList()); //Cada propiedad conserva TODOS sus mensajes.

            return Response<T>.Invalid(errors);
        }
    }
}
