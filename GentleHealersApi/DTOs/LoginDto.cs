namespace GentleHealersApi.DTOs
{
    /// <summary>Data transfer object for user login credentials.</summary>
    public class LoginDto
    {
        /// <summary>Gets or sets the email of the user logging in.</summary>
        public required string Email { get; set; }

        /// <summary>Gets or sets the password of the user.</summary>
        public required string Password { get; set; }
    }
}