using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ticketing.Query.Domain.Entities;

namespace Ticketing.Query.Infrastructure.Persistence
{
    public class TicketTypeConfiguration : IEntityTypeConfiguration<TicketType>
    {
        public void Configure(EntityTypeBuilder<TicketType> builder)
        {
            builder.ToTable("ticket_types");
            builder.HasKey(e => e.Id);
        }
    }
}
