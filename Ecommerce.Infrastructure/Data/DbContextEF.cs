using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Interceptors;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Text;

namespace Ecommerce.Infrastructure.Data
{
    public class DbContextEF : DbContext
    {
        private readonly IConfiguration _conf;
        private readonly string _connectionString;

        public readonly AuditableEntitySaveChangesInterceptor auditableEntitySaveChangesInterceptor;

        public DbSet<Customer> Customers { get; set; }

        public DbContextEF(IConfiguration configuration, AuditableEntitySaveChangesInterceptor auditableEntitySaveChangesInterceptor)
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
            optionsBuilder.UseSqlServer(_connectionString, builder => builder.EnableRetryOnFailure());
            optionsBuilder.AddInterceptors(auditableEntitySaveChangesInterceptor);
            optionsBuilder.EnableSensitiveDataLogging(); //Esto permite ver en consola las consultas SQL generadas por EF Core, útil para depuración.
        }

        public IDbConnection CreateConnection() => new SqlConnection(_connectionString);

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // Aquí puedes agregar lógica adicional antes de guardar los cambios, si es necesario.
            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}
