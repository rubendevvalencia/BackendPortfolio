using Ecommerce.Application.Dto;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Application.Validator
{
    public class CustomerDtoValidator : AbstractValidator<CustomerDto>
    {
        public CustomerDtoValidator()
        {
            RuleFor(x => x.CompanyName)
                .NotEmpty().WithMessage("Company name is required.")
                .MaximumLength(100).WithMessage("Company name cannot exceed 100 characters.");
            RuleFor(x => x.ContactName)
                .NotEmpty().WithMessage("Contact name is required.")
                .MaximumLength(100).WithMessage("Contact name cannot exceed 100 characters.");
            RuleFor(x => x.ContactTitle)
                .NotEmpty().WithMessage("Contact title is required.")
                .MaximumLength(100).WithMessage("Contact title cannot exceed 100 characters.");
            RuleFor(x => x.Address)
                .NotEmpty().WithMessage("Address is required.")
                .MaximumLength(200).WithMessage("Address cannot exceed 200 characters.");
            RuleFor(x => x.City)
                .NotEmpty().WithMessage("City is required.")
                .MaximumLength(100).WithMessage("City cannot exceed 100 characters.");
            RuleFor(x => x.Region)
                .NotEmpty().WithMessage("Region is required.")
                .MaximumLength(100).WithMessage("Region cannot exceed 100 characters.");
            RuleFor(x => x.PostalCode)
                .NotEmpty().WithMessage("Postal code is required.")
                .MaximumLength(20).WithMessage("Postal code cannot exceed 20 characters.");
            RuleFor(x => x.Country)
                .NotEmpty().WithMessage("Country is required.")
                .MaximumLength(100).WithMessage("Country cannot exceed 100 characters.");
            RuleFor(x => x.Phone)
                .NotEmpty().WithMessage("Phone is required.")
                .MaximumLength(20).WithMessage("Phone cannot exceed 20 characters.");
            RuleFor(x => x.Fax)
                .NotEmpty().WithMessage("Fax is required.")
                .MaximumLength(20).WithMessage("Fax cannot exceed 20 characters.");
        }
    }
}
