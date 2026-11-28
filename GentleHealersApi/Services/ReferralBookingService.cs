using System.Data;
using GentleHealersApi.DTOs.Referral;
using GentleHealersApi.Services.Interfaces;
using Microsoft.Data.SqlClient;

namespace GentleHealersApi.Services;

public class ReferralBookingService(ISqlConnectionFactory connections)
{
    public async Task<IReadOnlyList<BookableReferralTreatment>> GetTreatmentsAsync(
        int userId, CancellationToken cancellationToken)
    {
        await using var connection = await connections.GetAuthenticatedConnectionAsync(userId);
        await using var command = new SqlCommand("""
            SELECT treatment.ReferralTreatmentId, code.CareCode, code.Description, treatment.DurationQuarters * 15
            FROM dbo.ReferralTreatment treatment
            JOIN dbo.Referral referral ON referral.ReferralId = treatment.ReferralId
            JOIN dbo.Patient patient ON patient.PatientId = referral.PatientId
            JOIN dbo.TreatmentCode code ON code.TreatmentCodeId = treatment.TreatmentCodeId
            WHERE patient.UserId = @UserId AND treatment.Status = 'Open'
                AND referral.ExpiresAt > dbo.GetLocalTime()
            ORDER BY referral.ExpiresAt, treatment.ReferralTreatmentId;
            """, connection);
        command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;

        var treatments = new List<BookableReferralTreatment>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            treatments.Add(new(reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetInt32(3)));
        }
        return treatments;
    }

    public async Task<IReadOnlyList<ReferralAppointmentSlot>> GetSlotsAsync(
        int userId, int referralTreatmentId, CancellationToken cancellationToken)
    {
        await using var connection = await connections.GetAuthenticatedConnectionAsync(userId);
        await using var command = CreateSlotCommand(connection, userId, referralTreatmentId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var slots = new List<ReferralAppointmentSlot>();
        while (await reader.ReadAsync(cancellationToken))
        {
            slots.Add(new(reader.GetInt32(0), reader.GetInt32(1), reader.GetDateTime(2),
                reader.GetString(3), reader.GetString(4)));
        }
        return slots;
    }

    public async Task<bool> BookAsync(
        int userId, int referralTreatmentId, BookReferralAppointmentRequest request, CancellationToken cancellationToken)
    {
        await using var connection = await connections.GetAuthenticatedConnectionAsync(userId);

        // Keep the availability checks and the write in one transaction to prevent overlapping reservations.
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
        await using var availability = CreateSlotCommand(connection, userId, referralTreatmentId, transaction, request);
        if (await availability.ExecuteScalarAsync(cancellationToken) is null)
            return false;

        await using var command = new SqlCommand("""
            INSERT INTO dbo.Appointment
                (ReferralTreatmentId, PatientId, SpecialistId, LocationId, DepartmentName,
                 StartTime, DurationQuarters, CreatedByUserId)
            SELECT treatment.ReferralTreatmentId, referral.PatientId, @SpecialistId, @LocationId, specialty.Name,
                   @StartTime, treatment.DurationQuarters, @UserId
            FROM dbo.ReferralTreatment treatment
            JOIN dbo.Referral referral ON referral.ReferralId = treatment.ReferralId
            JOIN dbo.TreatmentCode code ON code.TreatmentCodeId = treatment.TreatmentCodeId
            JOIN dbo.Specialty specialty ON specialty.SpecialtyId = code.SpecialtyId
            WHERE treatment.ReferralTreatmentId = @ReferralTreatmentId;

            DECLARE @AppointmentId int = CONVERT(int, SCOPE_IDENTITY());
            INSERT INTO dbo.AppointmentTreatment (AppointmentId, TreatmentCodeId, AddedByUserId)
            SELECT @AppointmentId, TreatmentCodeId, @UserId
            FROM dbo.ReferralTreatment
            WHERE ReferralTreatmentId = @ReferralTreatmentId;

            UPDATE dbo.ReferralTreatment
            SET Status = 'Scheduled'
            WHERE ReferralTreatmentId = @ReferralTreatmentId;
            """, connection, transaction);
        BindBookingParameters(command, userId, referralTreatmentId, request);
        await command.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private static SqlCommand CreateSlotCommand(
        SqlConnection connection, int userId, int referralTreatmentId,
        SqlTransaction? transaction = null, BookReferralAppointmentRequest? request = null)
    {
        var command = new SqlCommand("""
            DECLARE @Now datetime2 = dbo.GetLocalTime();
            DECLARE @NoticeHours int = (
                SELECT TRY_CONVERT(int, SettingValue)
                FROM dbo.Setting WHERE SettingKey = 'MinBookingNoticeHours'
            );
            IF @NoticeHours IS NULL OR @NoticeHours < 0 SET @NoticeHours = 24;

            -- An interval beyond the referral expiry cannot produce a valid slot.
            DECLARE @ExpiresAt datetime2 = (
                SELECT referral.ExpiresAt
                FROM dbo.ReferralTreatment treatment
                JOIN dbo.Referral referral ON referral.ReferralId = treatment.ReferralId
                JOIN dbo.Patient patient ON patient.PatientId = referral.PatientId
                WHERE treatment.ReferralTreatmentId = @ReferralTreatmentId
                    AND treatment.Status = 'Open' AND patient.UserId = @UserId
            );
            IF @ExpiresAt IS NULL OR DATEDIFF(hour, @Now, @ExpiresAt) < @NoticeHours RETURN;

            DECLARE @Earliest datetime2 = DATEADD(hour, @NoticeHours, @Now);
            DECLARE @QuarterEpoch datetime2 = '20000101';
            DECLARE @FirstQuarter datetime2 =
                DATEADD(minute, DATEDIFF(minute, @QuarterEpoch, @Earliest) / 15 * 15, @QuarterEpoch);
            IF @FirstQuarter < @Earliest SET @FirstQuarter = DATEADD(minute, 15, @FirstQuarter);

            WITH SlotStarts AS (
                SELECT COALESCE(@StartTime, @FirstQuarter) AS StartTime
                WHERE COALESCE(@StartTime, @FirstQuarter) >= @Earliest
                    AND COALESCE(@StartTime, @FirstQuarter) < @ExpiresAt
                UNION ALL
                SELECT DATEADD(minute, 15, StartTime)
                FROM SlotStarts
                WHERE @StartTime IS NULL AND DATEADD(minute, 15, StartTime) < @ExpiresAt
            )
            SELECT specialist.SpecialistId, location.LocationId, slot.StartTime, location.Name,
                   account.FirstName + ' ' + account.LastName
            FROM dbo.ReferralTreatment treatment
            JOIN dbo.Referral referral ON referral.ReferralId = treatment.ReferralId
            JOIN dbo.Patient patient ON patient.PatientId = referral.PatientId
            JOIN dbo.TreatmentCode code ON code.TreatmentCodeId = treatment.TreatmentCodeId
            JOIN dbo.SpecialistSpecialty specialty ON specialty.SpecialtyId = code.SpecialtyId
            JOIN dbo.Specialist specialist ON specialist.SpecialistId = specialty.SpecialistId
            JOIN dbo.AppUser account ON account.UserId = specialist.UserId AND account.Status = 'Active'
            CROSS JOIN SlotStarts slot
            JOIN dbo.LocationOpeningHours hours ON hours.DayOfWeek = DATEDIFF(day, '19000101', slot.StartTime) % 7 + 1
            JOIN dbo.Location location ON location.LocationId = hours.LocationId
            CROSS APPLY (VALUES (DATEADD(minute, treatment.DurationQuarters * 15, slot.StartTime))) duration(EndTime)
            WHERE treatment.ReferralTreatmentId = @ReferralTreatmentId
                AND treatment.Status = 'Open' AND patient.UserId = @UserId
                AND (@SpecialistId IS NULL OR specialist.SpecialistId = @SpecialistId)
                AND (@LocationId IS NULL OR location.LocationId = @LocationId)
                AND hours.IsClosed = 0
                AND CAST(slot.StartTime AS time) >= hours.OpeningTime
                AND CAST(duration.EndTime AS date) = CAST(slot.StartTime AS date)
                AND CAST(duration.EndTime AS time) <= hours.ClosingTime
                AND NOT EXISTS (
                    SELECT 1 FROM dbo.Appointment appointment
                    WHERE appointment.CancelledAt IS NULL
                        AND (appointment.SpecialistId = specialist.SpecialistId OR appointment.PatientId = patient.PatientId)
                        AND appointment.StartTime < duration.EndTime AND appointment.EndTime > slot.StartTime
                )
                AND NOT EXISTS (
                    SELECT 1 FROM dbo.SpecialistUnavailability unavailable
                    WHERE unavailable.SpecialistId = specialist.SpecialistId
                        AND unavailable.FromTime < duration.EndTime AND unavailable.ToTime > slot.StartTime
                )
            ORDER BY slot.StartTime, location.LocationId, specialist.SpecialistId
            OPTION (MAXRECURSION 0);
            """, connection, transaction);
        BindBookingParameters(command, userId, referralTreatmentId, request);
        return command;
    }

    private static void BindBookingParameters(
        SqlCommand command, int userId, int referralTreatmentId, BookReferralAppointmentRequest? request)
    {
        command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
        command.Parameters.Add("@ReferralTreatmentId", SqlDbType.Int).Value = referralTreatmentId;
        command.Parameters.Add("@SpecialistId", SqlDbType.Int).Value = (object?)request?.SpecialistId ?? DBNull.Value;
        command.Parameters.Add("@LocationId", SqlDbType.Int).Value = (object?)request?.LocationId ?? DBNull.Value;
        command.Parameters.Add("@StartTime", SqlDbType.DateTime2).Value = (object?)request?.StartTime ?? DBNull.Value;
    }
}
