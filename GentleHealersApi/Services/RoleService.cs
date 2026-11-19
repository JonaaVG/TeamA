using GentleHealersApi.DTOs;
using GentleHealersApi.Services.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data;

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

        public async Task<List<UserRoleDto>> GetAllUsersAsync()
        {
            var users = new List<UserRoleDto>();

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql = """
        SELECT u.UserId, u.FirstName, u.LastName, u.Email,
        r.RoleId, r.Name AS RoleName, u.Status
        FROM dbo.AppUser AS u
        INNER JOIN dbo.Role AS r ON r.RoleId = u.RoleId
        ORDER BY u.LastName, u.FirstName;
        """;

            await using var command = new SqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                users.Add(new UserRoleDto
                {
                    UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                    FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                    LastName = reader.GetString(reader.GetOrdinal("LastName")),
                    Email = reader.GetString(reader.GetOrdinal("Email")),
                    RoleId = reader.GetInt32(reader.GetOrdinal("RoleId")),
                    RoleName = reader.GetString(reader.GetOrdinal("RoleName")),
                    Status = reader.GetString(reader.GetOrdinal("Status"))
                });
            }

            return users;
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

        public async Task<bool> DeactivateUserAsync(int userId)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql = """
        UPDATE dbo.AppUser
        SET Status = N'Inactive'
        WHERE UserId = @UserId;
        """;

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;

            return await command.ExecuteNonQueryAsync() > 0;
        }
    }
}
