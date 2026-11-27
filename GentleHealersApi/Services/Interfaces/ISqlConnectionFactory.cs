using Microsoft.Data.SqlClient;

namespace GentleHealersApi.Services.Interfaces
{
    /// <summary>
    /// Provides methods for creating and opening SQL database connections authenticated for a specific user.
    /// </summary>
    public interface ISqlConnectionFactory
    {
        /// <summary>
        /// Creates and returns an open SQL connection without a user session context.
        /// </summary>
        Task<SqlConnection> GetConnectionAsync();

        /// <summary>
        /// Creates and returns an authenticated SQL connection asynchronously for the specified user.
        /// </summary>
        /// <param name="userId">The unique ID of the user for whom the connection is established.</param>
        /// <returns>An opened <see cref="SqlConnection"/> instance.</returns>
        Task<SqlConnection> GetConnectionAsync(int userId);
    }
}