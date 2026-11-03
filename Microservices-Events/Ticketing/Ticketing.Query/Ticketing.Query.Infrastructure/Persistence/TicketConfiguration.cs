using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ticketing.Query.Domain.Entities;

namespace Ticketing.Query.Infrastructure.Persistence;

public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("tickets");
        builder.HasKey(e => e.Id);

        builder
            .HasMany(t => t.Employees)
            .WithMany(t => t.Tickets)
            .UsingEntity(j => j.ToTable("ticket_employees")); // Tabla intermedia muchos a muchos entre Ticket y Employee, sin clase propia.
    }
}
