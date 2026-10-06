using Ecommerce.Application.Feature.Users.Commands.SignIn;
using Ecommerce.Domain.Entities.Jwt;
using Microsoft.AspNetCore.DataProtection;

namespace Ecommerce.Application.Mapping.SignInMapping
{
    public static class SignInMapping
    {
        public static string ConfigureMappingToWrapper(this User data, IDataProtector protector)
        {
            string firstName = protector.Unprotect(data.FirstName!);
            string lastName = protector.Unprotect(data.LastName!);
            return firstName + " " + lastName;
        }

    }
}


