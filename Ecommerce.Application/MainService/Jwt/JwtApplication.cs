using Ecommerce.Application.Dto.Jwt;
using Ecommerce.Application.Interface.Jwt;
using Ecommerce.Domain.Entities.Jwt;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Ecommerce.Application.MainService.Jwt
{
    public class JwtApplication : IJwtApplication
    {
        private readonly IConfiguration _confi;
        public JwtApplication(IConfiguration confi)
        {
            _confi = confi;
        }
        public string GenerateToken(User entity)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_confi["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, entity.UserName),
                new Claim(ClaimTypes.Email, entity.Email),
            };

            var token = new JwtSecurityToken(
                issuer: _confi["Jwt:Issuer"],
                audience: _confi["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

    }
}
