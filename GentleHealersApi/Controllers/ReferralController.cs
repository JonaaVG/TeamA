using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using GentleHealersApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using GentleHealersApi.DTOs.Referral;
using GentleHealersApi.Extensions;
using GentleHealersApi.DTOs;

namespace GentleHealersApi.Controllers
{
    /// <summary>
    /// Controller for managing referrals for patients. Provides endpoints to create referrals for existing patients.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ReferralController : ControllerBase
    {
        private readonly IReferralService _referralService;
        private readonly IEmailService _emailService;
        private readonly ILogger<ReferralController> _logger;
        private readonly string _portalUrl;
        public ReferralController(
            IReferralService referralService,
            IEmailService emailService,
            ILogger<ReferralController> logger,
            IConfiguration configuration)
        {
            _referralService = referralService;
            _emailService = emailService;
            _logger = logger;
            _portalUrl = configuration["AppSettings:PortalBaseUrl"]
                ?? throw new InvalidOperationException("AppSettings:PortalBaseUrl ontbreekt in de configuratie.");
        }
        //////////////////////////////////////////////////////
        ///                                                ///
        ///         Referral Creation Endpoint             ///
        ///                                                ///
        //////////////////////////////////////////////////////

        /// <summary>
        /// Creates referrals for a specific patient. The authenticated user must be a General Practitioner to access this endpoint. The request body should contain the referral details.
        /// </summary>
        /// <param name="patientId">The ID of the patient for whom to create referrals.</param>
        /// <param name="request">The referral details.</param>
        /// <returns>The created referrals.</returns>
        [Authorize(Roles = "GeneralPractitioner")]
        [HttpPost("patient/{patientId}/referrals")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<IActionResult> AddReferral(
            [FromRoute] int patientId,
            [FromBody] CreateReferralsRequest request)
        {
            try
            {
                var userId = User.GetUserIdFromClaims();
                var result = await _referralService.CreateReferralsAsync(patientId, request.Referrals, userId);

                var emailSent = await TrySendReferralEmailAsync(patientId, result);

                return StatusCode(StatusCodes.Status201Created, new
                {
                    referralTreatmentIds = result.Treatments.Select(t => t.ReferralTreatmentId),
                    emailSent
                });
            }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (SqlException ex) when (ex.Number == 547) { return BadRequest(new { message = "Onbekende patiënt of behandelcode." }); }
            catch (SqlException ex) when (ex.Number is 2627 or 2601) { return Conflict(new { message = "Er ging iets mis, probeer het opnieuw." }); }
        }
        /// <summary>
        /// Attempts to send a referral email to the patient after referrals have been created. Logs any exceptions that occur during the email sending process.
        /// </summary>
        /// <param name="patientId">The ID of the patient to whom to send the email.</param>
        /// <param name="result">The result of the referral creation process.</param>
        /// <returns>A task representing the asynchronous operation, with a boolean indicating whether the email was sent successfully.</returns>
        private async Task<bool> TrySendReferralEmailAsync(int patientId, CreateReferralsResult result)
        {
            try
            {
                await _emailService.SendAsync(new EmailRequest(
                    Type: EmailType.Referral,
                    ToEmail: result.PatientEmail,
                    FirstName: result.PatientFirstName,
                    Link: _portalUrl,
                    RoleName: "Patient",
                    ReferralCount: result.Treatments.Count));

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Doorverwijsmail naar patiënt {PatientId} kon niet worden verzonden.", patientId);
                return false;
            }
        }

        //////////////////////////////////////////////////////
        ///                                                ///
        ///         Referral View Endpoint                 ///
        ///                                                ///
        //////////////////////////////////////////////////////

        /// <summary>
        /// Gets the open referral treatments of the authenticated patient.
        /// </summary>
        /// <returns>A list of referral treatments that can still be scheduled.</returns>
        /// <summary>
        /// Gets the open referral treatments of a patient. A patient can only view their own referrals; administrative staff can view those of any patient.
        /// </summary>
        /// <param name="patientId">The ID of the patient.</param>
        /// <returns>A list of referral treatments that can still be scheduled.</returns>
        [Authorize(Roles = "Patient,AdministrativeStaff")]
        [HttpGet("patient/{patientId}/referrals")]
        [ProducesResponseType(typeof(List<ReferralOverviewDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPatientReferrals([FromRoute] int patientId)
        {
            try
            {
                var userId = User.GetUserIdFromClaims();
                var isStaff = User.IsInRole("AdministrativeStaff");

                var result = await _referralService.GetOpenReferralsAsync(patientId, userId, isStaff);
                return Ok(result);
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

    }
}