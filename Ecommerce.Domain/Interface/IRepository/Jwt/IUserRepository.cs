using Ecommerce.Domain.Entities.Jwt;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Domain.Interface.IRepository.Jwt
{
    public interface IUserRepository
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByUserNameAsync(string userName);
        Task<bool> CreateUserAsync(User user);
        Task<bool> CheckPassAsync(User user, string password);
    }
}
