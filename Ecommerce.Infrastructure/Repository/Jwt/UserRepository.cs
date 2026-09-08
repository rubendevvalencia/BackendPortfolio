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
        public async Task<bool> CheckPassAsync(User user, string password)
        {
            var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
            return await Task.FromResult(verificationResult == PasswordVerificationResult.Success);
        }

        public async Task<bool> CreateUserAsync(User entity)
        {
            User userTransform = new User
            {
                FirstName = entity.FirstName,
                LastName = entity.LastName,
                Email = entity.Email,
                UserName = entity.UserName,
                PasswordHash = _passwordHasher.HashPassword(entity, entity.PasswordHash)
            };
            var result = await _dbContext.Users.AddAsync(userTransform);
            if(result != null) return true;
            return false;
        }

        public async Task<User?> GetByEmailAsync(string email) => await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
        public async Task<User?> GetByUserNameAsync(string userName) => await _dbContext.Users.FirstOrDefaultAsync(u => u.UserName == userName);
    }
}
