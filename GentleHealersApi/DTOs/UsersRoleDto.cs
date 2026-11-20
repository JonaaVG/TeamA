namespace GentleHealersApi.DTOs;

/// <summary>Represents a user and their assigned role in the role-management list.</summary>
public class UserRoleDto
{
    /// <summary>Gets or sets the user's unique identifier.</summary>
    public int UserId { get; set; }
    /// <summary>Gets or sets the user's first name.</summary>
    public string FirstName { get; set; } = string.Empty;
    /// <summary>Gets or sets the user's last name.</summary>
    public string LastName { get; set; } = string.Empty;
    /// <summary>Gets or sets the user's email address.</summary>
    public string Email { get; set; } = string.Empty;
    /// <summary>Gets or sets the identifier of the user's assigned role.</summary>
    public int RoleId { get; set; }
    /// <summary>Gets or sets the name of the user's assigned role.</summary>
    public string RoleName { get; set; } = string.Empty;
    /// <summary>Gets or sets the account status, such as Active or Inactive.</summary>
    public string Status { get; set; } = string.Empty;
}