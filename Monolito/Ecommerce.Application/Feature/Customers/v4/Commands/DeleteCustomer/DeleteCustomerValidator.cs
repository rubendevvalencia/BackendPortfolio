using FluentValidation;

namespace Ecommerce.Application.Feature.Customers.v4.Commands.DeleteCustomer
{
    public class DeleteCustomerValidator : AbstractValidator<DeleteCustomerCommand>
    {
        public DeleteCustomerValidator()
        {
            //En v3 este "id <= 0" lo comprobaba el controller a mano; en v4 vive aqui y el controller solo delega.
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Id must be greater than 0");
        }
    }
}
