using System.Security.Claims;

namespace GentleHealersApi.Extensions
{
    /// <summary>
    /// Provides extension methods for extracting user identity and claims from a <see cref="ClaimsPrincipal"/>.
    /// </summary>
    public static class ClaimsPrincipalExtensions
    {
        /// <summary>
        /// Extracts and parses the user ID from the claims principal using standard identifier claims (NameIdentifier or "sub").
        /// </summary>
        /// <param name="user">The claims principal representing the currently authenticated user.</param>
        /// <returns>The parsed integer user ID.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when the user identity claim is missing, empty, or not a valid integer.</exception>
        public static int GetUserIdFromClaims(this ClaimsPrincipal user)
        {
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                              ?? user.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                throw new UnauthorizedAccessException("Invalid or missing user identity claim.");
            }

            return userId;
        }
    }
}