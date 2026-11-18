namespace GentleHealersApi.Models;

/// <summary>
/// Maps the dbo.Role table. Kept separate from RoleDto, which is an API response type.
/// </summary>
public sealed class Role
{
    public int RoleId { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<AppUser> Users { get; set; } = new List<AppUser>();
}
