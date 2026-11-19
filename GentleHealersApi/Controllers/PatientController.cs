using Microsoft.AspNetCore.Mvc;
using GentleHealersApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
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

        public PatientController(IPatientService patientService)
        {
            _patientService = patientService;
        }

        [ProducesResponseType(typeof(PatientCreatedDto), StatusCodes.Status201Created)]
        public async Task<ActionResult<PatientCreatedDto>> AddPatient(
        int userId,
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