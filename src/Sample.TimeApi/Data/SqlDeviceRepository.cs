using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Sample.TimeApi.Data
{

    /// <summary>
    /// Sql device repository
    /// </summary>
    /// <remarks>
    /// To get started Sql Server on docker is a good option:
    /// docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=Pass@Word1" -p 1433:1433 -d mcr.microsoft.com/mssql/server:2019-GA-ubuntu-16.04
    /// </remarks>
    public class SqlDeviceRepository : IDeviceRepository
    {
        private readonly string connectionString;
        private readonly ILogger logger;

        public SqlDeviceRepository(IConfiguration configuration, ILogger<SqlDeviceRepository> logger)
        {
            this.connectionString = configuration["SqlConnectionString"];
            this.logger = logger;
        }

        public async Task<DateTime> GetTimeFromSqlAsync()
        {
            using var conn = new SqlConnection(this.connectionString);
            await conn.OpenAsync();

            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("Getting date from Sql Server");
            }

            using var cmd = new SqlCommand("SELECT GETDATE()", conn);
            var res = await cmd.ExecuteScalarAsync();

            return (DateTime)res;
        }
    }
}
