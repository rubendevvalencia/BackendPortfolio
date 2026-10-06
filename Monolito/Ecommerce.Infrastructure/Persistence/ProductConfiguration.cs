using Ecommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecommerce.Infrastructure.Persistence
{
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.ToTable("Products");
            builder.HasKey(p => p.Id); // Configura la propiedad Id como clave primaria de la entidad Product.

            builder.Property(p => p.Name)
                .IsRequired() // Indica que la propiedad Name es obligatoria (no puede ser nula).
                .HasMaxLength(100); // Establece una longitud máxima de 100 caracteres para la propiedad Name.

            builder.Property(p => p.Description)
                .HasMaxLength(500); // Establece una longitud máxima de 500 caracteres para la propiedad Description.

            builder.Property(p => p.Price)
                .IsRequired(); // Indica que la propiedad Price es obligatoria (no puede ser nula).

            builder.Property(p => p.StockQuantity)
                .IsRequired(); // Indica que la propiedad StockQuantity es obligatoria (no puede ser nula).

            builder.Property(p => p.Category)
                .IsRequired(); // Configura la relación entre Product y Category.

            builder.Property(p => p.CategoryId)
                .IsRequired(); // Indica que la propiedad CategoryId es obligatoria (no puede ser nula).

           
            
        }

    }
}


