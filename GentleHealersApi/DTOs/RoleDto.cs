namespace GentleHealersApi.DTOs
{
    /// <summary>Represents a role returned by or sent to the role-management API.</summary>
    public class RoleDto
    {
        /// <summary>Gets or sets the unique identifier of the role.</summary>
        public int RoleId { get; set; }

        /// <summary>Gets or sets the display name of the role.</summary>
        public string Name { get; set; } = string.Empty;
    }
}
