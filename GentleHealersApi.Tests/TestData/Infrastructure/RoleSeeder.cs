using Microsoft.Data.SqlClient;
using GentleHealersApi.Tests.TestData.Models;

namespace GentleHealersApi.Tests.TestData.Infrastructure
{
    public sealed class RoleSeeder
    {
        private readonly string _connectionString;

        public RoleSeeder(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void EnsureAllRolesExist()
        {
            // Ensure every enum role exists with its exact RoleId in dbo.Role
            foreach (RoleType role in Enum.GetValues<RoleType>())
            {
                EnsureRoleExists(role);
            }
        }

        public void EnsureRoleExists(RoleType role)
        {
            int roleId = (int)role;
            string roleName = role.ToString();

            const string checkSql = """
                SELECT COUNT(1)
                FROM dbo.Role
                WHERE RoleId = @RoleId;
                """;

            const string insertSql = """
                SET IDENTITY_INSERT dbo.Role ON;

                INSERT INTO dbo.Role (RoleId, Name)
                VALUES (@RoleId, @Name);

                SET IDENTITY_INSERT dbo.Role OFF;
                """;

            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            using (var checkCommand = new SqlCommand(checkSql, connection))
            {
                checkCommand.Parameters.AddWithValue("@RoleId", roleId);
                int exists = Convert.ToInt32(checkCommand.ExecuteScalar());

                if (exists > 0)
                {
                    return;
                }
            }

            using var insertCommand = new SqlCommand(insertSql, connection);
            insertCommand.Parameters.AddWithValue("@RoleId", roleId);
            insertCommand.Parameters.AddWithValue("@Name", roleName);
            insertCommand.ExecuteNonQuery();
        }
    }
}