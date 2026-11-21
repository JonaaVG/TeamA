using System.Data;
using GentleHealersApi.Extensions;
using GentleHealersApi.Tests.TestData.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace GentleHealersApi.Tests.TestData.Infrastructure;

public sealed class ActivationTestData : IDisposable
{
    private readonly string _connectionString;

    public int UserId { get; }
    public string Email { get; }
    public string RawToken { get; }
    public RoleType Role { get; private set; }

    public ActivationTestData(RoleType role = RoleType.Patient)
    {
        _connectionString = TestConfiguration.Configuration
            .GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Missing test database connection string.");

        new RoleSeeder(_connectionString).EnsureRoleExists(role);
        Role = role;
        Email = $"activation_{Guid.NewGuid():N}@gentlehealers-test.nl";
        RawToken = TokenHelper.GenerateRawToken();

        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            const string insertUser = """
                INSERT INTO dbo.AppUser
                    (RoleId, Email, PasswordHash, FirstName, LastName, Status, CreatedAt, LastLoginAt)
                OUTPUT INSERTED.UserId
                VALUES
                    (@RoleId, @Email, @PasswordHash, @FirstName, @LastName, 'Invited', SYSUTCDATETIME(), NULL);
                """;

            using (var command = new SqlCommand(insertUser, connection, transaction))
            {
                command.Parameters.Add("@RoleId", SqlDbType.Int).Value = (int)role;
                command.Parameters.Add("@Email", SqlDbType.NVarChar, 255).Value = Email;
                command.Parameters.Add("@PasswordHash", SqlDbType.NVarChar, 255).Value =
                    BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N"));
                command.Parameters.Add("@FirstName", SqlDbType.NVarChar, 100).Value = "Activation";
                command.Parameters.Add("@LastName", SqlDbType.NVarChar, 100).Value = "Test";
                UserId = Convert.ToInt32(command.ExecuteScalar());
            }

            const string insertToken = """
                INSERT INTO dbo.UserToken (UserId, Type, TokenHash, UsedAt)
                VALUES (@UserId, 'Activation', @TokenHash, NULL);
                """;

            using (var command = new SqlCommand(insertToken, connection, transaction))
            {
                command.Parameters.Add("@UserId", SqlDbType.Int).Value = UserId;
                command.Parameters.Add("@TokenHash", SqlDbType.NVarChar, 255).Value =
                    TokenHelper.HashToken(RawToken);
                command.ExecuteNonQuery();
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public void ExpireToken()
    {
        ExecuteTokenUpdate("UPDATE dbo.UserToken SET CreatedAt = @Value WHERE UserId = @UserId AND Type = 'Activation';", DateTime.UtcNow.AddYears(-1));
    }

    public void MarkTokenUsed()
    {
        ExecuteTokenUpdate("UPDATE dbo.UserToken SET UsedAt = @Value WHERE UserId = @UserId AND Type = 'Activation';", DateTime.UtcNow.AddMinutes(-1));
    }

    public string GetUserStatus()
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        using var command = new SqlCommand("SELECT Status FROM dbo.AppUser WHERE UserId = @UserId;", connection);
        command.Parameters.Add("@UserId", SqlDbType.Int).Value = UserId;
        return (string?)command.ExecuteScalar()
            ?? throw new InvalidOperationException("The activation test user was not found.");
    }

    public DateTime? GetTokenUsedAt()
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        using var command = new SqlCommand("SELECT UsedAt FROM dbo.UserToken WHERE UserId = @UserId AND Type = 'Activation';", connection);
        command.Parameters.Add("@UserId", SqlDbType.Int).Value = UserId;
        var result = command.ExecuteScalar();
        return result is null or DBNull ? null : Convert.ToDateTime(result);
    }

    public string GetRoleName()
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        const string query = "SELECT r.Name FROM dbo.AppUser u INNER JOIN dbo.Role r ON r.RoleId = u.RoleId WHERE u.UserId = @UserId;";
        using var command = new SqlCommand(query, connection);
        command.Parameters.Add("@UserId", SqlDbType.Int).Value = UserId;
        return (string?)command.ExecuteScalar()
            ?? throw new InvalidOperationException("The activation test user role was not found.");
    }

    public void Dispose()
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            using (var command = new SqlCommand(
                "DELETE FROM dbo.UserToken WHERE UserId = @UserId;", connection, transaction))
            {
                command.Parameters.Add("@UserId", SqlDbType.Int).Value = UserId;
                command.ExecuteNonQuery();
            }

            using (var command = new SqlCommand(
                "DELETE FROM dbo.AppUser WHERE UserId = @UserId;", connection, transaction))
            {
                command.Parameters.Add("@UserId", SqlDbType.Int).Value = UserId;
                command.ExecuteNonQuery();
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private void ExecuteTokenUpdate(string sql, DateTime value)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@Value", SqlDbType.DateTime2).Value = value;
        command.Parameters.Add("@UserId", SqlDbType.Int).Value = UserId;
        command.ExecuteNonQuery();
    }
}
