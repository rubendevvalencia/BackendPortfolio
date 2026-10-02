using Ecommerce.Application.Dto.Jwt;
using Ecommerce.Application.Feature.Users.Commands.SignUp;
using Ecommerce.Domain.Entities.Jwt;
using Microsoft.AspNetCore.DataProtection;

namespace Ecommerce.Application.Mapping.SignUpMapping
{
    public static class SignUpMapping
    {
        public static User ConfigureMappingToWrapper(this SignUpCommand profileCommand, IDataProtector protector)
        {
            User user = new ()
            {
                FirstName = protector.Protect(profileCommand.FirstName!),
                LastName = protector.Protect(profileCommand.LastName!),
                Email = profileCommand.Email!,
                UserName = profileCommand.UserName!,
            };
            return user;
        }

        public static User ConfigureMappingToWrapper(this SignUpDto profileCommand, IDataProtector protector)
        {
            User user = new ()
            {
                FirstName = protector.Protect(profileCommand.FirstName!),
                LastName = protector.Protect(profileCommand.LastName!),
                Email = protector.Protect(profileCommand.Email!),
            };
            return user;
        }
        
    }
}


