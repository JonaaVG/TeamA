
using System.Data;
using GentleHealersApi.DTOs.Patient;
using GentleHealersApi.Extensions;
using GentleHealersApi.Services.Interfaces;
using Microsoft.Data.SqlClient;

namespace GentleHealersApi.Services
{
    public class PatientService : IPatientService
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public PatientService(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<PatientCreatedDto> AddPatientAsync(
            CreatePatientDto dto,
            int practitionerUserId)
        {
            // 1. Check the BSN
            if (!IsValidBsn(dto.Bsn))
                throw new ArgumentException(
                    "Het BSN is ongeldig volgens de elfproef.");

            using var connection =
                await _connectionFactory.GetAuthenticatedConnectionAsync(
                    practitionerUserId);

            // 2. Find the practice ID for the practitioner
            int practiceId;

            var practiceQuery = @"
                SELECT PracticeId
                FROM dbo.GeneralPractitioner
                WHERE UserId = @UserId";

            using (var pCmd = new SqlCommand(practiceQuery, connection))
            {
                pCmd.Parameters.AddWithValue(
                    "@UserId", practitionerUserId);

                var result = await pCmd.ExecuteScalarAsync();

                if (result == null)
                    throw new UnauthorizedAccessException(
                        "Huisarts niet gevonden of geen rechten.");

                practiceId = Convert.ToInt32(result);
            }

            // 3. Check if the user exists and has the Patient role
            var userQuery = @"
                SELECT COUNT(1)
                FROM dbo.AppUser u
                INNER JOIN dbo.Role r ON u.RoleId = r.RoleId
                WHERE u.UserId = @UserId
                  AND r.Name = 'Patient'";

            using (var userCmd = new SqlCommand(userQuery, connection))
            {
                userCmd.Parameters.AddWithValue("@UserId", dto.UserId);

                int exists = Convert.ToInt32(
                    await userCmd.ExecuteScalarAsync());

                if (exists == 0)
                    throw new ArgumentException(
                        "De gebruiker bestaat niet of heeft geen Patient-rol.");
            }

            // 4. Check if the BSN already exists
            if (await ExistsAsync(
                connection,
                "SELECT 1 FROM dbo.Patient WHERE Bsn = @Val",
                "@Val",
                dto.Bsn))
            {
                throw new InvalidOperationException(
                    "Er bestaat al een patiënt met dit BSN.");
            }

            // 5. Create the Patient record
            using var transaction = connection.BeginTransaction();

            try
            {
                int newPatientId;

                var insertPatientQuery = @"
                    INSERT INTO dbo.Patient
                        (UserId, PracticeId, Bsn, DateOfBirth)
                    VALUES
                        (@UserId, @PracticeId, @Bsn, @DateOfBirth);

                    SELECT SCOPE_IDENTITY();";

                using (var patCmd = new SqlCommand(
                    insertPatientQuery, connection, transaction))
                {
                    patCmd.Parameters.AddWithValue("@UserId", dto.UserId);
                    patCmd.Parameters.AddWithValue("@PracticeId", practiceId);
                    patCmd.Parameters.AddWithValue("@Bsn", dto.Bsn);

                    patCmd.Parameters.Add("@DateOfBirth", SqlDbType.Date)
                        .Value = dto.DateOfBirth.ToDateTime(TimeOnly.MinValue);

                    newPatientId = Convert.ToInt32(
                        await patCmd.ExecuteScalarAsync());
                }

                await transaction.CommitAsync();

                // 6. Return the created patient record
                return new PatientCreatedDto
                {
                    PatientId = newPatientId,
                    UserId = dto.UserId
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // Check if a record exists
        private static async Task<bool> ExistsAsync(
            SqlConnection conn,
            string query,
            string paramName,
            object value)
        {
            using var cmd = new SqlCommand(query, conn);

            cmd.Parameters.AddWithValue(paramName, value);

            var result = await cmd.ExecuteScalarAsync();

            return result != null;
        }

        // Dutch BSN validation using the 11-proof algorithm
        private static bool IsValidBsn(string bsn)
        {
            if (string.IsNullOrWhiteSpace(bsn) ||
                bsn.Length != 9 ||
                !bsn.All(char.IsAsciiDigit))
            {
                return false;
            }

            int sum = 0;

            for (int i = 0; i < 8; i++)
            {
                sum += (bsn[i] - '0') * (9 - i);
            }

            sum -= (bsn[8] - '0');

            return sum > 0 && sum % 11 == 0;
        }
    }
}
