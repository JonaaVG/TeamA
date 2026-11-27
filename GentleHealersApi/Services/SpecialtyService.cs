using GentleHealersApi.DTOs;
using GentleHealersApi.Services.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data;

namespace GentleHealersApi.Services
{
    /// <summary>Performs specialty operations using SQL Server.</summary>
    public class SpecialtyService : ISpecialtyService
    {
        /// <summary>The connection string used to access the application database.</summary>
        private readonly ISqlConnectionFactory _connectionFactory;
        private readonly IConfiguration _configuration;
        /// <summary>Creates the service with the configured default database connection.</summary>
        /// <param name="configuration">Application configuration containing the connection string.</param>
        public SpecialtyService(ISqlConnectionFactory connectionFactory, IConfiguration configuration)
        {
            _connectionFactory = connectionFactory;
            _configuration = configuration;
        }
        /// <summary>
        /// Retrieves a list of specialties from the database for the specified user.
        /// This method uses an authenticated SQL connection to ensure that the user has access to the data
        /// </summary>
        /// <param name="userId">The ID of the user for whom to retrieve specialties.</param>
        /// <returns>a list of specialties for the specified user</returns>
        public async Task<IEnumerable<SpecialtyDto>> GetSpecialtiesAsync(int userId)
        {
            using var connection = await _connectionFactory.GetAuthenticatedConnectionAsync(userId);
            using var cmd = new SqlCommand(
                "SELECT SpecialtyId, Name FROM dbo.Specialty ORDER BY Name", connection);

            var results = new List<SpecialtyDto>();
            using var reader = await cmd.ExecuteReaderAsync();

            var idOrdinal = reader.GetOrdinal("SpecialtyId");
            var nameOrdinal = reader.GetOrdinal("Name");

            while (await reader.ReadAsync())
            {
                results.Add(new SpecialtyDto
                {
                    SpecialtyId = reader.GetInt32(idOrdinal),
                    Name = reader.GetString(nameOrdinal)
                });
            }

            return results;
        }
    }
}
