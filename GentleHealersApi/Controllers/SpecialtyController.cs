using GentleHealersApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GentleHealersApi.Extensions;

namespace GentleHealersApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "GeneralPractitioner")]
    public class SpecialtiesController : ControllerBase
    {
        private readonly ISpecialtyService _specialtyService;

        public SpecialtiesController(ISpecialtyService specialtyService)
        {
            _specialtyService = specialtyService;
        }

        /// <summary>
        /// Returns a list of specialties associated with the authenticated user.
        /// </summary>
        /// <returns>A list of specialties for the authenticated user.</returns>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userId = User.GetUserIdFromClaims();
            return Ok(await _specialtyService.GetSpecialtiesAsync(userId));
        }
    }
}