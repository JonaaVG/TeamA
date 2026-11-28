using System.Data;
using GentleHealersApi.Extensions;
using GentleHealersApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace GentleHealersApi.Controllers;

[ApiController]
[Route("api/appointments/specialist")]
[Authorize(Roles = "Specialist")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class SpecialistAppointmentsController(ISqlConnectionFactory connectionFactory) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SpecialistAppointment>>> GetAppointments(
        CancellationToken cancellationToken)
    {
        int userId;
        try
        {
            userId = User.GetUserIdFromClaims();
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }

        await using var connection = await connectionFactory.GetAuthenticatedConnectionAsync(userId);
        await using var command = new SqlCommand("""
            SELECT a.AppointmentId, u.FirstName, u.LastName, a.StartTime,
                   CASE WHEN r.ReferralId IS NOT NULL THEN t.Note END AS Note
            FROM dbo.Appointment AS a
            INNER JOIN dbo.Specialist AS s ON s.SpecialistId = a.SpecialistId
            INNER JOIN dbo.Patient AS p ON p.PatientId = a.PatientId
            INNER JOIN dbo.AppUser AS u ON u.UserId = p.UserId
            LEFT JOIN dbo.ReferralTreatment AS t ON t.ReferralTreatmentId = a.ReferralTreatmentId
            LEFT JOIN dbo.Referral AS r ON r.ReferralId = t.ReferralId AND r.PatientId = a.PatientId
            WHERE s.UserId = @UserId AND a.CancelledAt IS NULL
            ORDER BY a.StartTime, a.AppointmentId;
            """, connection);
        command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var appointments = new List<SpecialistAppointment>();
        while (await reader.ReadAsync(cancellationToken))
        {
            appointments.Add(new SpecialistAppointment(
                reader.GetInt32(0),
                $"{reader.GetString(1)} {reader.GetString(2)}",
                reader.GetDateTime(3),
                reader.IsDBNull(4) ? null : reader.GetString(4)));
        }

        return Ok(appointments);
    }

    public sealed record SpecialistAppointment(int AppointmentId, string PatientName, DateTime StartTime, string? Note);
}
