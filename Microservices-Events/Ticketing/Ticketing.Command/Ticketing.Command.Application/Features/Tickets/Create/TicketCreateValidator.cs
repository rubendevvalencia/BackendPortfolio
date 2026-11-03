using FluentValidation;

namespace Ticketing.Command.Application.Features.Tickets.Create
{
    public class TicketCreateValidator : AbstractValidator<TicketCreateCommand>
    {
        public TicketCreateValidator()
        {
            RuleFor(x => x.UserName)
                .NotEmpty().WithMessage("Product name is required.")
                .MaximumLength(100).WithMessage("UserName name cannot exceed 100 characters.");

            RuleFor(x => x.ErrorType)
                .NotEmpty().WithMessage("Product name is required.")
                .MaximumLength(100).WithMessage("ErrorType name cannot exceed 100 characters.");

            RuleFor(x => x.ErrorDetail)
                .NotEmpty().WithMessage("Product name is required.")
                .MaximumLength(400).WithMessage("ErrorDetail name cannot exceed 100 characters.");
        }
    }
}
