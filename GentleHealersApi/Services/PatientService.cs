
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

        public async Task<PatientCreatedDto> AddPatientAsync(CreatePatientDto dto, int practitionerUserId)
        {
            if (!IsValidBsn(dto.Bsn))
                throw new ArgumentException("Het BSN is ongeldig volgens de elfproef.");
            ValidateDateOfBirth(dto.DateOfBirth);
            using var connection = await _connectionFactory.GetConnectionAsync(practitionerUserId);
            using var transaction = connection.BeginTransaction();
            try
            {
                var result = await AddPatientAsync(dto, practitionerUserId, connection, transaction);
                await transaction.CommitAsync();
                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<PatientCreatedDto> AddPatientAsync(
            CreatePatientDto dto, int practitionerUserId,
            SqlConnection connection, SqlTransaction transaction)
        {
            if (!IsValidBsn(dto.Bsn))
                throw new ArgumentException("Het BSN is ongeldig volgens de elfproef.");
            ValidateDateOfBirth(dto.DateOfBirth);
            // 2. PracticeId
            int practiceId;
            using (var pCmd = new SqlCommand(@"
        SELECT PracticeId FROM dbo.GeneralPractitioner WHERE UserId = @UserId",
                connection, transaction))
            {
                pCmd.Parameters.AddWithValue("@UserId", practitionerUserId);
                var result = await pCmd.ExecuteScalarAsync();
                if (result == null)
                    throw new UnauthorizedAccessException("Huisarts niet gevonden of geen rechten.");
                practiceId = Convert.ToInt32(result);
            }

            // 3. User bestaat + Patient-rol (ziet nu ook de net aangemaakte user, binnen dezelfde transactie)
            using (var userCmd = new SqlCommand(@"
        SELECT COUNT(1)
        FROM dbo.AppUser u
        INNER JOIN dbo.Role r ON u.RoleId = r.RoleId
        WHERE u.UserId = @UserId AND r.Name = 'Patient'",
                connection, transaction))
            {
                userCmd.Parameters.AddWithValue("@UserId", dto.UserId);
                if (Convert.ToInt32(await userCmd.ExecuteScalarAsync()) == 0)
                    throw new ArgumentException("De gebruiker bestaat niet of heeft geen Patient-rol.");
            }

            // 4. BSN bestaat al?
            if (await ExistsAsync(connection, transaction,
                "SELECT 1 FROM dbo.Patient WHERE Bsn = @Val", "@Val", dto.Bsn))
                throw new InvalidOperationException("Er bestaat al een patiënt met dit BSN.");

            // 5. Insert Patient
            int newPatientId;
            using (var patCmd = new SqlCommand(@"
        INSERT INTO dbo.Patient (UserId, PracticeId, Bsn, DateOfBirth)
        VALUES (@UserId, @PracticeId, @Bsn, @DateOfBirth);
        SELECT SCOPE_IDENTITY();", connection, transaction))
            {
                patCmd.Parameters.AddWithValue("@UserId", dto.UserId);
                patCmd.Parameters.AddWithValue("@PracticeId", practiceId);
                patCmd.Parameters.AddWithValue("@Bsn", dto.Bsn);
                patCmd.Parameters.Add("@DateOfBirth", SqlDbType.Date)
                    .Value = dto.DateOfBirth.ToDateTime(TimeOnly.MinValue);
                newPatientId = Convert.ToInt32(await patCmd.ExecuteScalarAsync());
            }

            return new PatientCreatedDto { PatientId = newPatientId, UserId = dto.UserId };
        }

        private static async Task<bool> ExistsAsync(
            SqlConnection conn, SqlTransaction tx, string query, string paramName, object value)
        {
            using var cmd = new SqlCommand(query, conn, tx);
            cmd.Parameters.AddWithValue(paramName, value);
            return await cmd.ExecuteScalarAsync() != null;
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

        private const int MaxAgeYears = 150;

        private static void ValidateDateOfBirth(DateOnly dateOfBirth)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);

            if (dateOfBirth > today)
                throw new ArgumentException("De geboortedatum mag niet in de toekomst liggen.");

            if (dateOfBirth < today.AddYears(-MaxAgeYears))
                throw new ArgumentException($"De geboortedatum mag niet meer dan {MaxAgeYears} jaar geleden zijn.");
        }
    }
}
