using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Ticketing.Query.Domain.Entities;

namespace Ticketing.Query.Infrastructure.Data
{
    public class TicketDbContext : DbContext
    {
        private readonly string _connectionString;
        public DbSet<Employee> Employees {get; set;}
        public DbSet<Ticket> Tickets {get; set;}

        public TicketDbContext(DbContextOptions<TicketDbContext> options, string connectionString):base(options)
        {
            _connectionString = connectionString;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly()); //Fluent API -> Llamamos a las configuraciones de las entidades (CustomerConfiguration, ProductConfiguration, etc.) que heredan de IEntityTypeConfiguration<T> y están en el mismo ensamblado que DbContextEF.
            base.OnModelCreating(modelBuilder);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //Si AddDbContext ya configuró el proveedor (DI o herramientas de EF), no lo volvemos a configurar.
           //if (!optionsBuilder.IsConfigured)
           //{
           //    optionsBuilder.UseApplicationServiceProvider(_connectionString, builder => builder.EnableRetryOnFailure());
           //}
           //optionsBuilder.AddInterceptors(_auditableEntitySaveChangesInterceptor);
        } 
    }
    
}


