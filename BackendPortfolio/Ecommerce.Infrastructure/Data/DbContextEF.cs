using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Entities.Jwt;
using Ecommerce.Infrastructure.Interceptors;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Text;

using Microsoft.Extensions.Hosting;

namespace Ecommerce.Infrastructure.Data
{
    public class DbContextEF : DbContext
    {
        private readonly IConfiguration _conf;
        private readonly string _connectionString;

        public readonly AuditableEntitySaveChangesInterceptor auditableEntitySaveChangesInterceptor;

        public DbSet<Customer> Customers { get; set; }
        public DbSet<User> Users { get; set; }

        public DbContextEF(DbContextOptions<DbContextEF> options, IConfiguration configuration, AuditableEntitySaveChangesInterceptor auditableEntitySaveChangesInterceptor)
            : base(options)
        {
            _conf = configuration;
            _connectionString = _conf.GetConnectionString("EcommerceDb");
            this.auditableEntitySaveChangesInterceptor = auditableEntitySaveChangesInterceptor;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
            base.OnModelCreating(modelBuilder);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //Si AddDbContext ya configuró el proveedor (DI o herramientas de EF), no lo volvemos a configurar.
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(_connectionString, builder => builder.EnableRetryOnFailure());
            }
            optionsBuilder.AddInterceptors(auditableEntitySaveChangesInterceptor);

            
           
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // Aquí puedes agregar lógica adicional antes de guardar los cambios, si es necesario.
            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}
