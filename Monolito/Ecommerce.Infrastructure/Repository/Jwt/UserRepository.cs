using Ecommerce.Domain.Entities.Jwt;
using Ecommerce.Domain.Interface.IRepository.Jwt;
using Ecommerce.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Infrastructure.Repository.Jwt
{
    public class UserRepository : IUserRepository
    {
        private readonly DbContextEF _dbContext;
        private readonly IPasswordHasher<User> _passwordHasher;

        public UserRepository(DbContextEF dbContext, IPasswordHasher<User> passwordHasher)
        {
            _dbContext = dbContext;
            _passwordHasher = passwordHasher;
        }
       
        public bool CheckPass(User user, string password)
        {
            var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
            var esCorrecta = verificationResult == PasswordVerificationResult.Success;

            return esCorrecta;
        }

      
        public async Task CreateUserAsync(User entity, string password)
        {
            var passHash = _passwordHasher.HashPassword(entity, password);

            var userTransform = new User
            {
                FirstName = entity.FirstName,
                LastName = entity.LastName,
                Email = entity.Email,
                UserName = entity.UserName,
                PasswordHash = passHash
            };

            await _dbContext.Users.AddAsync(userTransform);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
            return user;
        }

        public async Task<User?> GetByUserNameAsync(string userName)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserName == userName);
            return user;
        }
    }
}
