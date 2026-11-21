using GentleHealersApi.DTOs;
using GentleHealersApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GentleHealersApi.Controllers
{
    /// <summary>Exposes role-management operations for system administrators.</summary>
    [ApiController]
    [Route("api/admin/roles")]
    [Authorize(Roles = "SystemAdministrator")]
    public class RoleMangementController : ControllerBase
    {
        private readonly IRoleService _roleService;

        /// <summary>Creates the controller with its role-management service.</summary>
        /// <param name="roleService">Service for retrieving roles and managing user accounts.</param>
        public RoleMangementController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        /// <summary>Returns the roles available for assignment.</summary>
        /// <returns>HTTP 200 with the role list.</returns>
        [HttpGet("roles")]
        public async Task<ActionResult<List<RoleDto>>> GetRoles()
        {
            return Ok(await _roleService.GetAllRolesAsync());
        }

        /// <summary>Returns users with their current role and account status.</summary>
        /// <returns>HTTP 200 with the user list.</returns>
        [HttpGet("users")]
        public async Task<ActionResult<List<UserRoleDto>>> GetUsers()
        {
            return Ok(await _roleService.GetAllUsersAsync());
        }

        /// <summary>Assigns an existing role to the specified user.</summary>
        /// <param name="userId">The identifier of the user to update.</param>
        /// <param name="request">The requested role identifier.</param>
        /// <returns>HTTP 200 when updated, or HTTP 404 when the user or role is missing.</returns>
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

        /// <summary>Deactivates the specified account by setting its status to Inactive.</summary>
        /// <param name="userId">The identifier of the account to deactivate.</param>
        /// <returns>HTTP 200 when deactivated, or HTTP 404 when the user is missing.</returns>
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

        /// <summary>Activates the specified account by setting its status to Active.</summary>
        /// <param name="userId">The identifier of the account to activate.</param>
        /// <returns>HTTP 200 when activated, or HTTP 404 when the user is missing.</returns>
        [HttpPut("user/{userId:int}/activate")]
        public async Task<IActionResult> ActivateUser(int userId)
        {
            bool activated = await _roleService.ActivateUserAsync(userId);

            if (!activated)
            {
                return NotFound("Gebruiker niet gevonden.");
            }

            return Ok("Account geactiveerd.");
        }
    }
}
