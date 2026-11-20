using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;

namespace GentleHealersApi.Tests;

/// <summary>Feature-scoped test data and token setup for the role-management browser scenarios.</summary>
internal sealed class RoleManagementTestContext : IDisposable
{
    private readonly string _connectionString;
    private readonly Dictionary<string, UserSnapshot?> _originalUsers = new(StringComparer.OrdinalIgnoreCase);
    private readonly JsonDocument _settings;

    public RoleManagementTestContext()
    {
        var root = FindRepositoryRoot();
        var settingsPath = Path.Combine(root, "GentleHealersApi", "appsettings.Development.json");
        _settings = JsonDocument.Parse(File.ReadAllText(settingsPath));
        _connectionString = Environment.GetEnvironmentVariable("GENTLEHEALERS_TEST_DB_CONNECTION_STRING")
            ?? _settings.RootElement.GetProperty("ConnectionStrings").GetProperty("DefaultConnection").GetString()!;

        var dataSource = new SqlConnectionStringBuilder(_connectionString).DataSource.ToLowerInvariant();
        if (!(dataSource.Contains("localhost") || dataSource.Contains("127.0.0.1") || dataSource == "." || dataSource.Contains("(local)")))
        {
            throw new InvalidOperationException("Acceptance tests require a local, dedicated test SQL Server.");
        }
    }

    public string ClientBaseUrl => Environment.GetEnvironmentVariable("GENTLEHEALERS_TEST_CLIENT_URL") ?? "http://localhost:5173";
    public string ApiBaseUrl => Environment.GetEnvironmentVariable("GENTLEHEALERS_TEST_API_URL") ?? "http://localhost:5159";

    public TestAccount EnsureAccount(string email, string firstName, string lastName, string roleName, string status)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        var roleId = GetRoleId(connection, roleName);
        if (!_originalUsers.ContainsKey(email)) _originalUsers[email] = ReadUser(connection, email);

        var existing = ReadUser(connection, email);
        if (existing is null)
        {
            const string insertSql = """
                INSERT INTO dbo.AppUser (RoleId, Email, PasswordHash, FirstName, LastName, Status, CreatedAt, LastLoginAt)
                OUTPUT INSERTED.UserId
                VALUES (@RoleId, @Email, @PasswordHash, @FirstName, @LastName, @Status, SYSUTCDATETIME(), NULL);
                """;
            using var insert = new SqlCommand(insertSql, connection);
            AddAccountParameters(insert, email, firstName, lastName, roleId, status);
            var id = Convert.ToInt32(insert.ExecuteScalar());
            return new TestAccount(id, email, firstName, lastName, roleId, roleName);
        }

        const string updateSql = """
            UPDATE dbo.AppUser
            SET RoleId = @RoleId, FirstName = @FirstName, LastName = @LastName, Status = @Status
            WHERE UserId = @UserId;
            """;
        using var update = new SqlCommand(updateSql, connection);
        AddAccountParameters(update, email, firstName, lastName, roleId, status);
        update.Parameters.AddWithValue("@UserId", existing.UserId);
        update.ExecuteNonQuery();
        return new TestAccount(existing.UserId, email, firstName, lastName, roleId, roleName);
    }

    public int RoleId(string roleName)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        return GetRoleId(connection, roleName);
    }

    public AccountState ReadAccount(string email)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        const string sql = """
            SELECT u.RoleId, r.Name, u.Status
            FROM dbo.AppUser AS u
            INNER JOIN dbo.Role AS r ON r.RoleId = u.RoleId
            WHERE u.Email = @Email;
            """;
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Email", email);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) throw new InvalidOperationException($"Test account '{email}' no longer exists.");
        return new AccountState(reader.GetInt32(0), reader.GetString(1), reader.GetString(2));
    }

    public string CreateToken(TestAccount account, string roleName)
    {
        var jwt = _settings.RootElement.GetProperty("Jwt");
        var key = Encoding.ASCII.GetBytes(jwt.GetProperty("Key").GetString()!);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, account.UserId.ToString()),
            new Claim(ClaimTypes.Email, account.Email),
            new Claim(ClaimTypes.Role, roleName),
        };
        var signingCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            jwt.GetProperty("Issuer").GetString(),
            jwt.GetProperty("Audience").GetString(),
            claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: signingCredentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public void Dispose()
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            foreach (var (email, old) in _originalUsers)
            {
                if (old is null)
                {
                    using var delete = new SqlCommand("DELETE FROM dbo.AppUser WHERE Email = @Email", connection);
                    delete.Parameters.AddWithValue("@Email", email);
                    delete.ExecuteNonQuery();
                    continue;
                }

                const string restoreSql = """
                    UPDATE dbo.AppUser
                    SET RoleId = @RoleId, FirstName = @FirstName, LastName = @LastName, Email = @Email,
                        PasswordHash = @PasswordHash, Status = @Status, CreatedAt = @CreatedAt, LastLoginAt = @LastLoginAt
                    WHERE UserId = @UserId;
                    """;
                using var restore = new SqlCommand(restoreSql, connection);
                restore.Parameters.AddWithValue("@RoleId", old.RoleId);
                restore.Parameters.AddWithValue("@FirstName", old.FirstName);
                restore.Parameters.AddWithValue("@LastName", old.LastName);
                restore.Parameters.AddWithValue("@Email", old.Email);
                restore.Parameters.AddWithValue("@PasswordHash", (object?)old.PasswordHash ?? DBNull.Value);
                restore.Parameters.AddWithValue("@Status", old.Status);
                restore.Parameters.AddWithValue("@CreatedAt", old.CreatedAt);
                restore.Parameters.AddWithValue("@LastLoginAt", (object?)old.LastLoginAt ?? DBNull.Value);
                restore.Parameters.AddWithValue("@UserId", old.UserId);
                restore.ExecuteNonQuery();
            }
        }
        finally
        {
            _settings.Dispose();
        }
    }

    private static void AddAccountParameters(SqlCommand command, string email, string firstName, string lastName, int roleId, string status)
    {
        command.Parameters.AddWithValue("@RoleId", roleId);
        command.Parameters.AddWithValue("@Email", email);
        command.Parameters.AddWithValue("@PasswordHash", BCrypt.Net.BCrypt.HashPassword("MSTest-NotUsed-123!"));
        command.Parameters.AddWithValue("@FirstName", firstName);
        command.Parameters.AddWithValue("@LastName", lastName);
        command.Parameters.AddWithValue("@Status", status);
    }

    private static int GetRoleId(SqlConnection connection, string roleName)
    {
        using var command = new SqlCommand("SELECT RoleId FROM dbo.Role WHERE Name = @Name", connection);
        command.Parameters.AddWithValue("@Name", roleName);
        var roleId = command.ExecuteScalar();
        return roleId is null or DBNull ? throw new InvalidOperationException($"Required role '{roleName}' does not exist.") : Convert.ToInt32(roleId);
    }

    private static UserSnapshot? ReadUser(SqlConnection connection, string email)
    {
        const string sql = """
            SELECT UserId, RoleId, FirstName, LastName, Email, PasswordHash, Status, CreatedAt, LastLoginAt
            FROM dbo.AppUser WHERE Email = @Email;
            """;
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Email", email);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) return null;
        return new UserSnapshot(
            reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2), reader.GetString(3), reader.GetString(4),
            reader.IsDBNull(5) ? null : reader.GetString(5), reader.GetString(6), reader.GetDateTime(7),
            reader.IsDBNull(8) ? null : reader.GetDateTime(8));
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !Directory.Exists(Path.Combine(current.FullName, "GentleHealersApi"))) current = current.Parent;
        return current?.FullName ?? throw new DirectoryNotFoundException("Could not locate the repository root.");
    }

    private sealed record UserSnapshot(int UserId, int RoleId, string FirstName, string LastName, string Email, string? PasswordHash, string Status, DateTime CreatedAt, DateTime? LastLoginAt);
}

internal sealed record TestAccount(int UserId, string Email, string FirstName, string LastName, int RoleId, string RoleName);
internal sealed record AccountState(int RoleId, string RoleName, string Status);
