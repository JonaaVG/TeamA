using System;
using System.Data;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace GentleHealersApi.Tests.TestData.Factories
{
    public sealed class CreatedTestPatient
    {
        public int PatientId { get; init; }
        public int UserId { get; init; }
        public int PracticeId { get; init; }
        public string Bsn { get; init; } = string.Empty;
        public DateOnly DateOfBirth { get; init; }
    }

    public sealed class TestPatientFactory
    {
        private readonly string _connectionString;

        public TestPatientFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        // Inserts a test patient using direct SQL and sets SESSION_CONTEXT for RLS
        public async Task<CreatedTestPatient> CreateAsync(
            int userId,
            int practiceId,
            int actingUserId,
            DateOnly? dateOfBirth = null,
            string? bsn = null)
        {
            DateOnly resolvedDob = dateOfBirth ?? new DateOnly(1990, 5, 14);
            string resolvedBsn = bsn ?? await GenerateUniqueBsnAsync();

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Set SESSION_CONTEXT so PatientPolicy evaluates the acting user's role/scope
            await using (var ctxCmd = connection.CreateCommand())
            {
                ctxCmd.CommandText = "EXEC sys.sp_set_session_context @key = N'UserId', @value = @ActingUserId;";
                ctxCmd.Parameters.Add("@ActingUserId", SqlDbType.Int).Value = actingUserId;
                await ctxCmd.ExecuteNonQueryAsync();
            }

            // Adjust column list if your latest CreateDbGentleHealers.sql adds or renames columns on dbo.Patient
            const string insertSql = @"
                INSERT INTO dbo.Patient (UserId, PracticeId, Bsn, DateOfBirth)
                OUTPUT INSERTED.PatientId
                VALUES (@UserId, @PracticeId, @Bsn, @DateOfBirth);";

            await using var cmd = connection.CreateCommand();
            cmd.CommandText = insertSql;
            cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
            cmd.Parameters.Add("@PracticeId", SqlDbType.Int).Value = practiceId;
            cmd.Parameters.Add("@Bsn", SqlDbType.NVarChar, 20).Value = resolvedBsn;
            cmd.Parameters.Add("@DateOfBirth", SqlDbType.Date).Value = resolvedDob.ToDateTime(TimeOnly.MinValue);

            object? scalarId = await cmd.ExecuteScalarAsync();
            int patientId = Convert.ToInt32(scalarId);

            return new CreatedTestPatient
            {
                PatientId = patientId,
                UserId = userId,
                PracticeId = practiceId,
                Bsn = resolvedBsn,
                DateOfBirth = resolvedDob
            };
        }

        // Generates a valid BSN that does not yet exist in dbo.Patient
        public async Task<string> GenerateUniqueBsnAsync(int maxAttempts = 50)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                string candidate = GenerateValidBsn();

                await using var checkCmd = connection.CreateCommand();
                checkCmd.CommandText = "SELECT 1 FROM dbo.Patient WHERE Bsn = @Bsn;";
                checkCmd.Parameters.Add("@Bsn", SqlDbType.NVarChar, 20).Value = candidate;

                object? exists = await checkCmd.ExecuteScalarAsync();
                if (exists is null or DBNull)
                {
                    return candidate;
                }
            }

            throw new InvalidOperationException(
                $"Could not generate a unique BSN after {maxAttempts} attempts.");
        }

        // Computes a 9-digit BSN passing the elfproef used in dbo.checkBsn
        public static string GenerateValidBsn()
        {
            int[] weights = [9, 8, 7, 6, 5, 4, 3, 2];

            while (true)
            {
                int[] digits = new int[9];
                int weightedSum = 0;

                for (int i = 0; i < 8; i++)
                {
                    digits[i] = RandomNumberGenerator.GetInt32(0, 10);
                    weightedSum += digits[i] * weights[i];
                }

                // Last digit is multiplied by -1 in dbo.checkBsn, so remainder mod 11 must match
                int remainder = weightedSum % 11;
                if (remainder == 10)
                {
                    continue;
                }

                digits[8] = remainder;
                string bsn = string.Concat(digits);

                if (bsn != "000000000")
                {
                    return bsn;
                }
            }
        }
    }
}