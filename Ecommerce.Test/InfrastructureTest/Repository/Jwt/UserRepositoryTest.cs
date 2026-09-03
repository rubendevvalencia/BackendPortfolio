using Ecommerce.Domain.Entities.Jwt;
using Ecommerce.Infrastructure.Data;
using Ecommerce.Infrastructure.Interceptors;
using Ecommerce.Infrastructure.Repository.Jwt;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Ecommerce.Test.InfrastructureTest.Repository.Jwt
{
    public class UserRepositoryTest
    {
        public static DbContextEF CreateDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<DbContextEF>()
                .UseInMemoryDatabase(dbName)
                .Options;

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:EcommerceDb"] = "no-usada-en-tests"
                })
                .Build();

            return new DbContextEF(options, configuration, new AuditableEntitySaveChangesInterceptor());
        }

        private static string NewDbName() => Guid.NewGuid().ToString();

        [Fact]
        public async Task CreateUserAsync_ShouldReturnTrue_WhenUserIsCreatedAsync()
        {
            // Arrange
            var dbName = NewDbName();
            await using var dbContext = CreateDbContext(dbName);
            var passwordHasher = new PasswordHasher<User>();
            var userRepository = new UserRepository(dbContext, passwordHasher);
            var user = new User
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                UserName = "john",
                PasswordHash = "Password123!"
            };

            var result = await userRepository.CreateUserAsync(user);
            var savedUser = await dbContext.Users.SingleAsync(u => u.Email == user.Email);

            Assert.True(result);
            Assert.Equal(1, savedUser.Id);
            Assert.Equal("John", savedUser.FirstName);
            Assert.Equal("Doe", savedUser.LastName);
            Assert.Equal("john.doe@example.com", savedUser.Email);
            Assert.Equal("john", savedUser.UserName);
            Assert.NotEqual("Password123!", savedUser.PasswordHash);
        }


        [Theory]
        [InlineData("John", "Doe", "john.doe@example.com", "john", "Password123!", "Password123!", true)]
        [InlineData("Jane", "Smith", "jane.smith@example.com", "jane", "Password456!", "WrongPassword!", false)]
        [InlineData("Admin", "User", "admin@example.com", "admin", "Admin789!", "Admin789!", true)]
        public async Task CheckPassAsync_ShouldReturnExpectedResult_ForDifferentUsers(
            string firstName,
            string lastName,
            string email,
            string userName,
            string password,
            string passwordToCheck,
            bool expectedResult)
        {
            await using var context = CreateDbContext(NewDbName());
            var passwordHasher = new PasswordHasher<User>();
            var repository = new UserRepository(context, passwordHasher);
            var user = new User
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                UserName = userName
            };
            user.PasswordHash = passwordHasher.HashPassword(user, password);

            var result = await repository.CheckPassAsync(user, passwordToCheck);

            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public async Task GetByEmailAsync_ShouldReturnUser_WhenUserExists()
        {
            // Arrange
            var dbName = NewDbName();
            await using var dbContext = CreateDbContext(dbName);
            var passwordHasher = new PasswordHasher<User>();
            var userRepository = new UserRepository(dbContext, passwordHasher);
            var user = new User
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                UserName = "john"
            };
            user.PasswordHash = passwordHasher.HashPassword(user, "Password123!");
            await dbContext.Users.AddAsync(user);
            await dbContext.SaveChangesAsync();

            // Act
            var result = await userRepository.GetByEmailAsync(user.Email);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(user.Id, result.Id);
        }
        [Fact]
        public async Task GetByUserNameAsync_ShouldReturnUser_WhenUserExists()
        {
            // Arrange
            var dbName = NewDbName();
            await using var dbContext = CreateDbContext(dbName);
            var passwordHasher = new PasswordHasher<User>();
            var userRepository = new UserRepository(dbContext, passwordHasher);
            var user = new User
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                UserName = "john"
            };
            user.PasswordHash = passwordHasher.HashPassword(user, "Password123!");
            await dbContext.Users.AddAsync(user);
            await dbContext.SaveChangesAsync();

            // Act
            var result = await userRepository.GetByUserNameAsync(user.UserName);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(user.Id, result.Id);
        }
    }
}
