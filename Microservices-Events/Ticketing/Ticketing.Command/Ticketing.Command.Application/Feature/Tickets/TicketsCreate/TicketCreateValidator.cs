using FluentValidation;

namespace Ticketing.Command.Application.Feature.Tickets.TicketsCreate
{
    public class TicketCreateValidator : AbstractValidator<TicketCreateCommand>
    {
        public TicketCreateValidator()
        {
            RuleFor(x => x.UserName)
                .NotEmpty().WithMessage("Product name is required.")
                .MaximumLength(100).WithMessage("Product name cannot exceed 100 characters.");

            RuleFor(x => x.ErrorType)
                .NotEmpty().WithMessage("Product name is required.")
                .MaximumLength(100).WithMessage("Product name cannot exceed 100 characters.");

            RuleFor(x => x.ErrorDetail)
                .NotEmpty().WithMessage("Product name is required.")
                .MaximumLength(400).WithMessage("Product name cannot exceed 100 characters.");
        }
    }
}
