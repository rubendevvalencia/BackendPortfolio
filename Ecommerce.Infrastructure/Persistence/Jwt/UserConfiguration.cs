using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Entities.Jwt;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Infrastructure.Persistence.Jwt
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users");
            builder.HasKey(u => u.Id); // Configura la propiedad Id como clave primaria de la entidad User.

            builder.Property(u => u.FirstName)
                .IsRequired() // Indica que la propiedad FirstName es obligatoria (no puede ser nula).
                .HasMaxLength(100); // Establece una longitud máxima de 100 caracteres para la propiedad FirstName.

            builder.Property(u => u.LastName)
                .IsRequired() // Indica que la propiedad LastName es obligatoria (no puede ser nula).
                .HasMaxLength(100); // Establece una longitud máxima de 100 caracteres para la propiedad LastName.

            builder.Property(u => u.Email)
                .IsRequired() // Indica que la propiedad Email es obligatoria (no puede ser nula).
                .HasMaxLength(100); // Establece una longitud máxima de 100 caracteres para la propiedad Email.

            builder.HasIndex(u => u.Email).IsUnique(); // Crea un índice único en la propiedad Email para garantizar que no haya duplicados.        

            builder.Property(u => u.UserName)
                .IsRequired() // Indica que la propiedad UserName es obligatoria (no puede ser nula).
                .HasMaxLength(100); // Establece una longitud máxima de 100 caracteres para la propiedad UserName.

            builder.HasIndex(u => u.UserName).IsUnique(); // Crea un índice único en la propiedad UserName para garantizar que no haya duplicados.

            builder.Property(u => u.PasswordHash)
                .IsRequired() // Indica que la propiedad PasswordHash es obligatoria (no puede ser nula).
                .HasMaxLength(255); // 255 caracteres: un hash BCrypt ocupa 60 y un SHA-256 en hexadecimal 64.
        }

    }
}
