using Ecommerce.Domain.Entities.Jwt;
using Ecommerce.Domain.Interface.IRepository;
using Ecommerce.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Infrastructure.Repository
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
            var rowsAffected = await _dbContext.SaveChangesAsync();
            if (rowsAffected > 0) return true;
            return false;
        }

        public async Task<User?> GetByEmailAsync(string email) => await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
    }
}
