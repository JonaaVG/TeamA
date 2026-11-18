namespace GentleHealersApi.Models;

/// <summary>
/// Minimal mapping for role assignment. Other dbo.AppUser columns are intentionally
/// omitted so this model stays focused on the API's role operations.
/// </summary>
public sealed class AppUser
{
    public int UserId { get; set; }
    public int RoleId { get; set; }

    public Role Role { get; set; } = null!;
}
