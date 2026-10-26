using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Ticketing.Query.Domain.Entities;

namespace Ticketing.Query.Infrastructure.Data
{
    public class TicketDbContext : DbContext
    {
        public DbSet<Employee> Employees {get; set;}
        public DbSet<Ticket> Tickets {get; set;}

        public TicketDbContext(DbContextOptions<TicketDbContext> options):base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly()); //Fluent API -> Llamamos a las configuraciones de las entidades (CustomerConfiguration, ProductConfiguration, etc.) que heredan de IEntityTypeConfiguration<T> y están en el mismo ensamblado que DbContextEF.
            base.OnModelCreating(modelBuilder);
        }
    }
    
}


