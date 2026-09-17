using Ecommerce.Transversal.Common;

namespace Ecommerce.Application.Common.Behaviours.Exceptions
{
    //La lanza ValidationBehaviour cuando la peticion no cumple sus reglas.
    //Los errores van agrupados por propiedad ("City" -> ["City is required."]), igual que Response.Errors.
    public class ValidationExceptionCustom : Exception
    {
        public ICollection<BaseError> Errors { get; }

        public ValidationExceptionCustom() : base("Validation Failures")
        {
            Errors = new List<BaseError>();
        }

        public ValidationExceptionCustom(List<BaseError> errors) : this()
        {
            Errors = errors;
        }
    }
}
