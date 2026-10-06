using FluentValidation;

namespace Ecommerce.Application.Feature.ProductToCustomer
{
    public class ProductToCustomerValidator : AbstractValidator<ProductToCustomerCommand>
    {
        public ProductToCustomerValidator()
        {
            RuleFor(x => x.ProductId)
                .NotEmpty().WithMessage("ProductId is required.")
                .GreaterThan(0).WithMessage("ProductId must be greater than 0.");

            RuleFor(x => x.CustomerId)
                .NotEmpty().WithMessage("CustomerId is required.")
                .GreaterThan(0).WithMessage("CustomerId must be greater than 0.");
        }
    }
}


