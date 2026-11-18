using GentleHealersApi.DTOs;
namespace GentleHealersApi.Services.Interfaces
{
    public interface IRoleService
    {
        Task<List<RoleDto>> GetAllRolesAsync();
        Task<bool> ChangeRoleAsync(int userId, int roleId);
    }
}
