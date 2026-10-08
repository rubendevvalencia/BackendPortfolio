using Ecommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Reflection.Emit;

namespace Ecommerce.Infrastructure.Persistence;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    //Esta clase permite configurar la entidad Customer en el contexto de Entity Framework Core. 
    // Se utiliza para definir cómo se mapea la entidad a la base de datos, incluyendo restricciones, relaciones y otras configuraciones específicas.
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        builder.HasKey(c => c.Id); // Configura la propiedad Id como clave primaria de la entidad Customer.

        builder.Property(c => c.CompanyName)
            .IsRequired() // Indica que la propiedad CompanyName es obligatoria (no puede ser nula).
            .HasMaxLength(100); // Establece una longitud máxima de 100 caracteres para la propiedad CompanyName.

        builder.Property(c => c.ContactName)
            .HasMaxLength(50); // Establece una longitud máxima de 50 caracteres para la propiedad ContactName.

        builder.Property(c => c.ContactTitle)
            .HasMaxLength(50); // Establece una longitud máxima de 50 caracteres para la propiedad ContactTitle.

        builder.Property(c => c.Address)
            .HasMaxLength(200); // Establece una longitud máxima de 200 caracteres para la propiedad Address.

        builder.Property(c => c.City)
            .HasMaxLength(50); // Establece una longitud máxima de 50 caracteres para la propiedad City.

        builder.Property(c => c.Region)
            .HasMaxLength(50); // Establece una longitud máxima de 50 caracteres para la propiedad Region.

        builder.Property(c => c.PostalCode)
            .HasMaxLength(20); // Establece una longitud máxima de 20 caracteres para la propiedad PostalCode.

        builder.Property(c => c.Country)
            .HasMaxLength(50); // Establece una longitud máxima de 50 caracteres para la propiedad Country.

        builder.Property(c => c.Phone)
            .HasMaxLength(20); // Establece una longitud máxima de 20 caracteres para la propiedad Phone.

        builder.Property(c => c.Fax)
            .HasMaxLength(20); // Establece una longitud máxima de 20 caracteres para la propiedad Fax.

        builder.HasMany(c => c.Products) // Configura la relación uno a muchos entre Customer y Product.
            .WithMany(p => p.Customer) // Indica que la entidad Product no tiene una propiedad de navegación hacia Customer.
            .UsingEntity(j => j.ToTable("CustomerProducts")); // Configura la tabla intermedia "CustomerProducts" para la relación muchos a muchos entre Customer y Product.
    }


}
