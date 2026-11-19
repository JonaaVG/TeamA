using GentleHealersApi.DTOs;
using GentleHealersApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GentleHealersApi.Controllers
{
    [ApiController]
    [Route("api/admin/roles")]
    [Authorize(Roles = "SystemAdministrator")]
    public class RoleMangementController : ControllerBase
    {
        private readonly IRoleService _roleService;

        public RoleMangementController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        [HttpGet("roles")]
        public async Task<ActionResult<List<RoleDto>>> GetRoles()
        {
            return Ok(await _roleService.GetAllRolesAsync());
        }

        [HttpGet("users")]
        public async Task<ActionResult<List<UserRoleDto>>> GetUsers()
        {
            return Ok(await _roleService.GetAllUsersAsync());
        }

        [HttpPut("user/{userId:int}/role")]
        public async Task<IActionResult> ChangeUserRole(
            int userId,
            [FromBody] RoleDto request)
        {
            bool changed = await _roleService.ChangeRoleAsync(userId, request.RoleId);

            if (!changed)
            {
                return NotFound("Gebruiker of rol niet gevonden.");
            }

            return Ok("Rol succesvol gewijzigd.");
        }

        [HttpPut("user/{userId:int}/deactivate")]
        public async Task<IActionResult> DeactivateUser(int userId)
        {
            bool deactivated = await _roleService.DeactivateUserAsync(userId);

            if (!deactivated)
            {
                return NotFound("Gebruiker niet gevonden.");
            }

            return Ok("Account gedeactiveerd.");
        }
    }
}
