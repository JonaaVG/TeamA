using GentleHealersApi.DTOs;
namespace GentleHealersApi.Services.Interfaces
{
    public interface IRoleService
    {
        Task<List<RoleDto>> GetAllRolesAsync();
        Task<List<UserRoleDto>> GetAllUsersAsync();
        Task<bool> ChangeRoleAsync(int userId, int roleId);
    }
}
