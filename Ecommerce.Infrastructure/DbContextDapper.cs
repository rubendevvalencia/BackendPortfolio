using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Ecommerce.Infrastructure
{
    public class DbContextDapper
    {
        private readonly IConfiguration _conf;
        private readonly string _connectionString;

        public DbContextDapper(IConfiguration configuration)
        {
            _conf = configuration;
            _connectionString = _conf.GetConnectionString("EcommerceDb");
        }

        public IDbConnection CreateConnection() => new SqlConnection(_connectionString);
    }
}
