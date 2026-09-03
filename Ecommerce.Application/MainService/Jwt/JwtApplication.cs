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
        public (string, int) GenerateToken(User entity)
        {
            //Fail-fast: appsettings.json declara "Jwt:Key" vacia a proposito, el valor real llega de
            //User Secrets en desarrollo o de la variable de entorno Jwt__Key en despliegue. Sin este
            //control, una clave ausente revienta mas abajo con un error que no dice que configurar.
            var configuredKey = _confi["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(configuredKey))
            {
                throw new InvalidOperationException(
                    "No hay clave de firma 'Jwt:Key'. Configurala con: " +
                    "dotnet user-secrets set \"Jwt:Key\" \"<clave>\" --project Ecommerce");
            }

            //HMAC-SHA256 exige una clave de 256 bits como minimo; con menos, la propia libreria
            //rechaza la firma. El limite se mide en bytes, no en caracteres.
            var keyBytes = Encoding.UTF8.GetBytes(configuredKey);
            if (keyBytes.Length < 32)
            {
                throw new InvalidOperationException(
                    $"La clave 'Jwt:Key' tiene {keyBytes.Length} bytes y HMAC-SHA256 exige 32 o mas. " +
                    "Genera una aleatoria, no una frase escrita a mano.");
            }

            var key = new SymmetricSecurityKey(keyBytes);
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

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
            int expiresIn = (int)(token.ValidTo - DateTime.UtcNow).TotalSeconds;

            return (tokenString, expiresIn);
        }

    }
}
