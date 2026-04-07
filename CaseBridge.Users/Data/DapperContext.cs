using Microsoft.Data.SqlClient;
using Dapper;
using System.Data;

namespace CaseBridge_Users.Data
{
    public class DapperContext
    {
        private readonly string _connectionString;

        public DapperContext(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found in appsettings.json");
        }

        public IDbConnection CreateConnection()
            => new SqlConnection(_connectionString);
    }
}
