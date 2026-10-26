using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ticketing.Query.Domain.Entities;

namespace Ticketing.Query.Infrastructure.Persistence
{
    public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
    {
        public void Configure(EntityTypeBuilder<Employee> builder)
        {
            builder.ToTable("employees");
            builder.HasKey(e => e.Id);
        }
    }
}


