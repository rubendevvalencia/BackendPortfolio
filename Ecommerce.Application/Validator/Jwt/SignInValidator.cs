using Ecommerce.Application.Dto.Jwt;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Application.Validator.Jwt
{
    public class SignInValidator : AbstractValidator<SignInDto>
    {
    }
}
