using Microsoft.AspNetCore.Mvc;
using GentleHealersApi.Services.Interfaces;
using GentleHealersApi.DTOs.User;
using Microsoft.AspNetCore.Authorization;
using GentleHealersApi.Extensions;
using GentleHealersApi.Services.Email;
using GentleHealersApi.Services;
using GentleHealersApi.DTOs;
using GentleHealersApi.Exceptions;



namespace GentleHealersApi.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IEmailService _emailService;
        private readonly ILogger<UserController> _logger;
        public UserController(
            IUserService userService,
            IEmailService emailService,
            ILogger<UserController> logger)
        {
            _userService = userService;
            _emailService = emailService;
            _logger = logger;
        }
        /// <summary>
        /// Creates a new user account for the authenticated user.
        /// </summary>
        /// <param name="dto">The data transfer object for creating a new user.</param>
        /// <returns>The created user information.</returns>
        [Authorize(Roles = "GeneralPractitioner, SystemAdministrator")]
        [HttpPost("create")]
        [ProducesResponseType(typeof(UserCreatedDto), StatusCodes.Status201Created)]
        public async Task<ActionResult<UserCreatedDto>> CreateUser([FromBody] CreateUserDto dto)
        {
            try
            {
                int userId = User.GetUserIdFromClaims();
                var created = await _userService.CreateUserAsync(dto, userId);
                try
                {
                    await _emailService.SendAsync(new EmailRequest(
                        EmailType.Activation,
                        created.Email,
                        created.FirstName,
                        created.ActivationLink,
                        dto.RoleName));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send activation email to {Email}", created.Email);
                }

                return Created($"/api/users/{created.UserId}", created);
            }
            catch (EmailDeliveryException ex)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpPost("validate-activation-token")]
        public async Task<ActionResult<bool>> ValidateActivationToken(ValidateTokenDto dto)
        {
            var isValid = await _userService.ValidateActivationTokenAsync(dto.Email, dto.Token);

            if (!isValid)
            {
                return BadRequest("Activatielink is ongeldig of verlopen.");
            }

            return Ok(isValid);
        }

        [AllowAnonymous]
        [HttpPost("set-password")]
        public async Task<IActionResult> SetPassword([FromBody] SetPasswordDto dto)
        {
            var result = await _userService.SetPasswordAsync(dto);

            if (!result)
            {
                return BadRequest("Het instellen van het wachtwoord is mislukt. Controleer de activatielink en probeer het opnieuw.");
            }

            return Ok(new { message = "Wachtwoord succesvol ingesteld." });
        }
    }
}