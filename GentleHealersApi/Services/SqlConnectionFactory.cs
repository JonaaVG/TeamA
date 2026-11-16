using Microsoft.Data.SqlClient;
using System.Data;
using GentleHealersApi.Services.Interfaces;

namespace GentleHealersApi.Services
{
    /// <summary>
    /// Factory for creating SQL database connections with authenticated session context.
    /// </summary>
    public class SqlConnectionFactory : ISqlConnectionFactory
    {
        private readonly string _connectionString;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlConnectionFactory"/> class.
        /// </summary>
        /// <param name="configuration">The application configuration used to retrieve connection strings.</param>
        public SqlConnectionFactory(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        /// <summary>
        /// Opens a new SQL connection and sets the session context for Row-Level Security based on the user ID.
        /// </summary>
        /// <param name="userId">The ID of the user for session context.</param>
        /// <returns>An opened <see cref="SqlConnection"/> with configured session context.</returns>
        public async Task<SqlConnection> GetAuthenticatedConnectionAsync(int userId)
        {
            var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand("sp_set_session_context", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@key", "UserID");
            command.Parameters.AddWithValue("@value", userId);
            command.Parameters.AddWithValue("@read_only", 1);

            await command.ExecuteNonQueryAsync();
            return connection;
        }
    }
}