using GentleHealersApi.Services.Interfaces;
using GentleHealersApi.DTOs.Referral;
using Microsoft.Data.SqlClient;

namespace GentleHealersApi.Services
{
    public class ReferralService : IReferralService
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public ReferralService(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }
        /// <summary>
        /// Creates referrals for a specific patient. The authenticated user must be a General Practitioner to access this endpoint. The request body should contain the referral details.
        /// </summary>
        /// <param name="patientId">The ID of the patient for whom to create referrals.</param>
        /// <param name="items">The referral items.</param>
        /// <param name="practitionerUserId">The ID of the authenticated practitioner.</param>
        /// <returns>A list of the created referral treatment IDs.</returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task<CreateReferralsResult> CreateReferralsAsync(
    int patientId, IReadOnlyList<CreateReferralItemDto> items, int practitionerUserId)
        {
            // Validation checks
            if (items.Count == 0)
                throw new ArgumentException("Er is minimaal één behandelcode nodig.");

            if (items.Select(i => i.TreatmentCodeId).Distinct().Count() != items.Count)
                throw new ArgumentException("Dezelfde behandelcode staat meerdere keren in de lijst.");

            if (items.Any(i => i.DurationMinutes <= 0 || i.DurationMinutes % 15 != 0))
                throw new ArgumentException("De duur moet een veelvoud van 15 minuten zijn.");

            using var connection = await _connectionFactory.GetAuthenticatedConnectionAsync(practitionerUserId);
            using var transaction = connection.BeginTransaction();

            try
            {
                // 1. Get the GeneralPractitionerId for the authenticated user
                int generalPractitionerId;
                using (var cmd = new SqlCommand(@"
                    SELECT GeneralPractitionerId
                    FROM dbo.GeneralPractitioner
                    WHERE UserId = @UserId", connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@UserId", practitionerUserId);
                    var result = await cmd.ExecuteScalarAsync();
                    if (result is null)
                        throw new UnauthorizedAccessException("Deze gebruiker is geen huisarts.");
                    generalPractitionerId = (int)result;
                }
                // 1b. Get the patient's email and first name for sending the referral email
                string patientEmail, patientFirstName;
                using (var cmd = new SqlCommand(@"
                    SELECT u.Email, u.FirstName
                    FROM dbo.Patient p
                    JOIN dbo.AppUser u ON u.UserId = p.UserId
                    JOIN dbo.GeneralPractitioner gp ON gp.PracticeId = p.PracticeId
                    WHERE p.PatientId = @PatientId AND gp.GeneralPractitionerId = @GeneralPractitionerId",
                    connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@PatientId", patientId);
                    cmd.Parameters.AddWithValue("@GeneralPractitionerId", generalPractitionerId);
                    using var reader = await cmd.ExecuteReaderAsync();
                    if (!await reader.ReadAsync())
                        throw new ArgumentException("Onbekende patiënt.");
                    patientEmail = reader.GetString(0);
                    patientFirstName = reader.GetString(1);
                }
                // 2. Create referral record for the patient
                int referralId;
                using (var cmd = new SqlCommand(@"
                    INSERT INTO dbo.Referral (PatientId, GeneralPractitionerId)
                    OUTPUT INSERTED.ReferralId
                    VALUES (@PatientId, @GeneralPractitionerId)", connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@PatientId", patientId);
                    cmd.Parameters.AddWithValue("@GeneralPractitionerId", generalPractitionerId);
                    referralId = (int)(await cmd.ExecuteScalarAsync())!;
                }

                // 3. ReferralTreatment records for each item in the request
                var created = new List<CreatedReferralTreatment>();
                foreach (var item in items)
                {
                    using var cmd = new SqlCommand(@"
                        INSERT INTO dbo.ReferralTreatment
                            (ReferralId, TreatmentCodeId, DurationQuarters, Note)
                        OUTPUT INSERTED.ReferralTreatmentId
                        SELECT @ReferralId, TreatmentCodeId, @DurationQuarters, @Note
                        FROM dbo.TreatmentCode
                        WHERE TreatmentCodeId = @TreatmentCodeId AND IsActive = 1",
                        connection, transaction);

                    var note = string.IsNullOrWhiteSpace(item.Notes) ? null : item.Notes.Trim();

                    cmd.Parameters.AddWithValue("@ReferralId", referralId);
                    cmd.Parameters.AddWithValue("@TreatmentCodeId", item.TreatmentCodeId);
                    cmd.Parameters.AddWithValue("@DurationQuarters", item.DurationMinutes / 15);
                    cmd.Parameters.AddWithValue("@Note", (object?)note ?? DBNull.Value);

                    if (await cmd.ExecuteScalarAsync() is not int id)
                        throw new ArgumentException("Kies een beschikbare behandelcode.");
                    created.Add(new CreatedReferralTreatment(id));
                }

                transaction.Commit();
                return new CreateReferralsResult(patientEmail, patientFirstName, created);
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}
