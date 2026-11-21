using GentleHealersApi.DTOs.User;
using Microsoft.Data.SqlClient;

namespace GentleHealersApi.Services.Interfaces
{
    public interface IUserService
    {
        /// <summary>
        /// Creates a new user in the system asynchronously.
        /// </summary>
        /// <param name="dto">The data transfer object containing user information.</param>
        /// <param name="practitionerUserId">The unique ID of the practitioner creating the user.</param>
        /// <returns>The created user data transfer object.</returns>
        Task<UserCreatedDto> CreateUserAsync(CreateUserDto dto, int practitionerUserId);

        Task<UserCreatedDto> CreateUserAsync(
            CreateUserDto dto, SqlConnection connection, SqlTransaction transaction);
        Task DeleteUserAsync(int userId, int practitionerUserId);
    }
}