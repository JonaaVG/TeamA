using Microsoft.AspNetCore.Mvc;
using GentleHealersApi.Services.Interfaces;
using GentleHealersApi.DTOs;

namespace GentleHealersApi.Controllers
{
    /// <summary>
    /// Controller for handling user authentication and login operations.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthController"/> class.
        /// </summary>
        /// <param name="authService">The authentication service.</param>
        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Authenticates a user and generates a JWT token upon successful login.
        /// </summary>
        /// <param name="model">The data transfer object containing user login credentials.</param>
        /// <returns>An action result containing the authentication token or an unauthorized response.</returns>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            var token = await _authService.AuthenticateUserAsync(model.Email, model.Password);

            if (token == null)
            {
                return Unauthorized(new { success = false, message = "Inloggegevens onjuist of account is niet actief. Controleer je E-mailadres en wachtwoord." });
            }

            return Ok(new { success = true, token });
        }
    }
}