using System;
using System.Data;
using GentleHealersApi.Tests.TestData.Models;
using Microsoft.Data.SqlClient;

namespace GentleHealersApi.Tests.TestData.Factories
{
    public sealed class TestUserAccount
    {
        public int UserId { get; init; }
        public RoleType Role { get; init; }
        public int RoleId => (int)Role;
        public string RoleName => Role.ToString();
        public string Email { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
    }

    public sealed class TestUserFactory
    {
        private const string DefaultPassword = "TestPassword!2026";
        private readonly string _connectionString;
        private readonly TestPatientFactory _patientFactory;

        public TestUserFactory(string connectionString, TestPatientFactory? patientFactory = null)
        {
            _connectionString = connectionString;
            _patientFactory = patientFactory ?? new TestPatientFactory(connectionString);
        }

        public TestUserAccount CreateGeneralPractitioner(string status = "Active")
        {
            return CreateUser(RoleType.GeneralPractitioner, prefix: "gp", status: status);
        }

        public TestUserAccount CreatePatient(
            int practiceId,
            int actingUserId,
            string status = "Active",
            DateOnly? dateOfBirth = null,
            string? bsn = null)
        {
            // Create AppUser first so we have UserId to insert into dbo.Patient
            TestUserAccount user = CreateUser(
                role: RoleType.Patient,
                prefix: "patient",
                status: status);

            _patientFactory
                .CreateAsync(
                    userId: user.UserId,
                    practiceId: practiceId,
                    actingUserId: actingUserId,
                    dateOfBirth: dateOfBirth,
                    bsn: bsn)
                .GetAwaiter()
                .GetResult();

            return user;
        }

        public TestUserAccount CreateUser(RoleType role, string prefix, string status = "Active")
        {
            int roleId = (int)role;
            string uniqueId = Guid.NewGuid().ToString("N")[..8];
            string email = $"{prefix}_{uniqueId}@gentlehealers-test.nl";
            string firstName = "Test";
            string lastName = $"{prefix}_{uniqueId}";

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(DefaultPassword);

            const string insertSql = """
                INSERT INTO dbo.AppUser
                    (RoleId, Email, PasswordHash, FirstName, LastName, Status, CreatedAt, LastLoginAt)
                OUTPUT INSERTED.UserId
                VALUES
                    (@RoleId, @Email, @PasswordHash, @FirstName, @LastName, @Status, SYSUTCDATETIME(), NULL);
                """;

            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            using var command = new SqlCommand(insertSql, connection);
            command.Parameters.Add("@RoleId", SqlDbType.Int).Value = roleId;
            command.Parameters.Add("@Email", SqlDbType.NVarChar, 255).Value = email;
            command.Parameters.Add("@PasswordHash", SqlDbType.NVarChar, 255).Value = passwordHash;
            command.Parameters.Add("@FirstName", SqlDbType.NVarChar, 100).Value = firstName;
            command.Parameters.Add("@LastName", SqlDbType.NVarChar, 100).Value = lastName;
            command.Parameters.Add("@Status", SqlDbType.NVarChar, 50).Value = status;

            int userId = Convert.ToInt32(command.ExecuteScalar());

            return new TestUserAccount
            {
                UserId = userId,
                Role = role,
                Email = email,
                Password = DefaultPassword,
                FirstName = firstName,
                LastName = lastName,
                Status = status
            };
        }

    }
}