using Ticketing.Common.Common.Response;

namespace Ticketing.Command.Application.Common.Exceptions
{
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


