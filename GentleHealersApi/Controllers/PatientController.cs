using Microsoft.AspNetCore.Mvc;
using GentleHealersApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using GentleHealersApi.DTOs;
using GentleHealersApi.DTOs.Patient;
using GentleHealersApi.Extensions;

namespace GentleHealersApi.Controllers
{
    /// <summary>
    /// Controller for managing patients.
    /// </summary>
    [Authorize(Roles = "GeneralPractitioner")]
    [ApiController]
    [Route("api/[controller]")]
    public class PatientController : ControllerBase
    {
        private readonly IPatientService _patientService;
        private readonly IPatientRegistrationService _registrationService;
        private readonly IEmailService _emailService;
        private readonly ILogger<PatientController> _logger;

        public PatientController(
            IPatientService patientService,
            IPatientRegistrationService registrationService,
            IEmailService emailService,
            ILogger<PatientController> logger)
        {
            _patientService = patientService;
            _registrationService = registrationService;
            _emailService = emailService;
            _logger = logger;
        }

        /// <summary>
        /// Creates a user account and a patient record in one go.
        /// If the patient record cannot be created, the user is not kept.
        /// </summary>
        [Authorize(Roles = "GeneralPractitioner")]
        [HttpPost("register")]
        [ProducesResponseType(typeof(PatientCreatedDto), StatusCodes.Status201Created)]
        public async Task<ActionResult<PatientCreatedDto>> RegisterPatient(
            [FromBody] RegisterPatientDto dto)
        {
            try
            {
                int practitionerUserId = User.GetUserIdFromClaims();

                var (user, patient) = await _registrationService.RegisterPatientAsync(
                    dto,
                    practitionerUserId);

                // Only send the activation email after everything succeeded
                try
                {
                    await _emailService.SendAsync(new EmailRequest(
                        EmailType.Activation,
                        user.Email,
                        user.FirstName,
                        user.ActivationLink,
                        "Patient"));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send activation email to {Email}", user.Email);
                }

                return Created($"/api/patient/{patient.PatientId}", patient);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Adds a patient record for an existing user.
        /// </summary>
        [Authorize(Roles = "GeneralPractitioner")]
        [HttpPost]
        [ProducesResponseType(typeof(PatientCreatedDto), StatusCodes.Status201Created)]
        public async Task<ActionResult<PatientCreatedDto>> AddPatient(
            [FromBody] CreatePatientDto dto)
        {
            try
            {
                int practitionerUserId = User.GetUserIdFromClaims();

                var created = await _patientService.AddPatientAsync(
                    dto,
                    practitionerUserId
                );

                return Created(
                    $"/api/patient/{created.PatientId}",
                    created
                );
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
        }
    }
}