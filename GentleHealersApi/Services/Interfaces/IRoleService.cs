using GentleHealersApi.DTOs;
namespace GentleHealersApi.Services.Interfaces
{
    /// <summary>Defines operations for roles, users, and user account status.</summary>
    public interface IRoleService
    {
        /// <summary>Retrieves all roles ordered by name.</summary>
        /// <returns>The available roles.</returns>
        Task<List<RoleDto>> GetAllRolesAsync();
        /// <summary>Retrieves users with their assigned role and account status.</summary>
        /// <returns>The users shown on the role-management page.</returns>
        Task<List<UserRoleDto>> GetAllUsersAsync();
        /// <summary>Assigns an existing role to a user.</summary>
        /// <param name="userId">The user to update.</param>
        /// <param name="roleId">The existing role to assign.</param>
        /// <returns>True if a row was updated; otherwise, false.</returns>
        Task<bool> ChangeRoleAsync(int userId, int roleId);
        /// <summary>Sets a user's account status to Inactive.</summary>
        /// <param name="userId">The user to deactivate.</param>
        /// <returns>True if a row was updated; otherwise, false.</returns>
        Task<bool> DeactivateUserAsync(int userId);
        /// <summary>Sets a user's account status to Active.</summary>
        /// <param name="userId">The user to activate.</param>
        /// <returns>True if a row was updated; otherwise, false.</returns>
        Task<bool> ActivateUserAsync(int userId);
    }
}
