using GentleHealersApi.DTOs.Referral;
using GentleHealersApi.Extensions;
using GentleHealersApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace GentleHealersApi.Controllers;

[ApiController]
[Authorize(Roles = "Patient")]
[Route("api/patient/referral-treatments")]
public class ReferralBookingController(ReferralBookingService bookings) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BookableReferralTreatment>>> GetTreatments(CancellationToken cancellationToken)
    {
        var treatments = await bookings.GetTreatmentsAsync(User.GetUserIdFromClaims(), cancellationToken);
        return Ok(treatments);
    }

    [HttpGet("{referralTreatmentId:int}/slots")]
    public async Task<ActionResult<IReadOnlyList<ReferralAppointmentSlot>>> GetSlots(
        int referralTreatmentId, CancellationToken cancellationToken)
    {
        var slots = await bookings.GetSlotsAsync(User.GetUserIdFromClaims(), referralTreatmentId, cancellationToken);
        return Ok(slots);
    }

    [HttpPost("{referralTreatmentId:int}/appointment")]
    public async Task<IActionResult> Book(
        int referralTreatmentId, BookReferralAppointmentRequest request, CancellationToken cancellationToken)
    {
        if (request.SpecialistId <= 0 || request.LocationId <= 0 ||
            request.StartTime.Kind != DateTimeKind.Unspecified ||
            request.StartTime.Minute % 15 != 0 ||
            request.StartTime.Ticks % TimeSpan.TicksPerMinute != 0)
        {
            return BadRequest(new { message = "Kies een geldig tijdslot." });
        }

        try
        {
            var booked = await bookings.BookAsync(User.GetUserIdFromClaims(), referralTreatmentId, request, cancellationToken);
            if (booked)
                return NoContent();
        }
        catch (SqlException error) when (error.Number is 1205 or 50002 or 50003)
        {
            // An overlapping appointment or unavailability may have been added during the booking.
        }

        return Conflict(new { message = "Dit tijdslot is niet meer beschikbaar voor deze doorverwijzing." });
    }
}
