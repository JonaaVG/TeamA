namespace GentleHealersApi.Services.Interfaces
{
    /// <summary>
    /// Provides authentication services for verifying user credentials and issuing access tokens.
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Authenticates a user asynchronously based on their email and password.
        /// </summary>
        /// <param name="email">The email of the user attempting to log in.</param>
        /// <param name="password">The plaintext password provided by the user.</param>
        /// <returns>A JWT token string if authentication succeeds; otherwise, null.</returns>
        Task<string?> AuthenticateUserAsync(string email, string password);
    }
}