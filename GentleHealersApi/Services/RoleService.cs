using GentleHealersApi.DTOs;
using GentleHealersApi.Services.Interfaces;
using Microsoft.Data.SqlClient;

namespace GentleHealersApi.Services
{
    public class RoleService : IRoleService
    {
        private readonly string _connectionString;

        public RoleService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<List<RoleDto>> GetAllRolesAsync()
        {
            var roles = new List<RoleDto>();

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql =
                "SELECT RoleId, Name FROM dbo.Role ORDER BY Name";

            await using var command = new SqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                roles.Add(new RoleDto
                {
                    RoleId = reader.GetInt32(0),
                    Name = reader.GetString(1)
                });
            }

            return roles;
        }

        public async Task<bool> ChangeRoleAsync(int userId, int roleId)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql = """
                UPDATE u
                SET RoleId = @RoleId
                FROM dbo.AppUser AS u
                WHERE u.UserId = @UserId
                  AND EXISTS (
                      SELECT 1 FROM dbo.Role AS r
                      WHERE r.RoleId = @RoleId
                  );
                """;

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@RoleId", roleId);

            return await command.ExecuteNonQueryAsync() > 0;
        }
    }
}
