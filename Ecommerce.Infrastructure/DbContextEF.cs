using Ecommerce.Domain.Entity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Ecommerce.Infrastructure
{
    public class DbContextEF : DbContext
    {
        private readonly IConfiguration _conf;
        private readonly string _connectionString;

        public DbSet<Customer> Customers { get; set; }

        public DbContextEF(IConfiguration configuration)
        {
            _conf = configuration;
            _connectionString = _conf.GetConnectionString("EcommerceDb");
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured) optionsBuilder.UseSqlServer(_connectionString);
        }

        public IDbConnection CreateConnection() => new SqlConnection(_connectionString);
    }
}
