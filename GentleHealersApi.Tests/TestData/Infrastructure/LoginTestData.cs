using System.Data;
using GentleHealersApi.Tests.TestData.Factories;
using GentleHealersApi.Tests.TestData.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace GentleHealersApi.Tests.TestData.Infrastructure;

public sealed class LoginTestData : IDisposable
{
    private readonly string _connectionString;

    public TestUserAccount Account { get; }

    public LoginTestData()
    {
        _connectionString = TestConfiguration.Configuration
            .GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Missing test database connection string.");

        new RoleSeeder(_connectionString).EnsureRoleExists(RoleType.GeneralPractitioner);
        Account = new TestUserFactory(_connectionString).CreateGeneralPractitioner();
    }

    public void Dispose()
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        using var command = new SqlCommand(
            "DELETE FROM dbo.AppUser WHERE UserId = @UserId",
            connection);
        command.Parameters.Add("@UserId", SqlDbType.Int).Value = Account.UserId;
        command.ExecuteNonQuery();
    }
}
