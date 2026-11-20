using GentleHealersApi.DTOs.User;
using GentleHealersApi.Extensions;
using GentleHealersApi.Services.Interfaces;
using Microsoft.Data.SqlClient;

namespace GentleHealersApi.Services
{
    public class UserService : IUserService
    {
        private readonly ISqlConnectionFactory _connectionFactory;
        private readonly IConfiguration _configuration;

        public UserService(ISqlConnectionFactory connectionFactory, IConfiguration configuration)
        {
            _connectionFactory = connectionFactory;
            _configuration = configuration;
        }

        public async Task<UserCreatedDto> CreateUserAsync(CreateUserDto dto, int practitionerUserId)
        {
            using var connection = await _connectionFactory.GetAuthenticatedConnectionAsync(practitionerUserId);
            using var transaction = connection.BeginTransaction();
            try
            {
                var result = await CreateUserAsync(dto, connection, transaction);
                await transaction.CommitAsync();
                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // Nieuwe overload: commit/rollback gebeurt door de aanroeper
        public async Task<UserCreatedDto> CreateUserAsync(
            CreateUserDto dto, SqlConnection connection, SqlTransaction transaction)
        {
            var email = dto.Email.Trim().ToLowerInvariant();

            // 1. Email check
            using (var checkCmd = new SqlCommand(
                "SELECT COUNT(1) FROM dbo.AppUser WHERE Email = @Email", connection, transaction))
            {
                checkCmd.Parameters.AddWithValue("@Email", email);
                if (Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0)
                    throw new InvalidOperationException("Er bestaat al een account met dit e-mailadres.");
            }

            // 2. RoleId
            int roleId;
            using (var roleCmd = new SqlCommand(
                "SELECT RoleId FROM dbo.Role WHERE Name = @RoleName", connection, transaction))
            {
                roleCmd.Parameters.AddWithValue("@RoleName", dto.RoleName);
                var result = await roleCmd.ExecuteScalarAsync();
                if (result == null)
                    throw new ArgumentException($"De rol '{dto.RoleName}' bestaat niet.");
                roleId = (int)result;
            }

            // 3. Token
            var rawToken = TokenHelper.GenerateRawToken();
            var tokenHash = TokenHelper.HashToken(rawToken);

            // 4. Insert AppUser
            int newUserId;
            using (var userCmd = new SqlCommand(@"
        INSERT INTO dbo.AppUser (RoleId, FirstName, LastName, Email)
        VALUES (@RoleId, @FirstName, @LastName, @Email);
        SELECT SCOPE_IDENTITY();", connection, transaction))
            {
                userCmd.Parameters.AddWithValue("@RoleId", roleId);
                userCmd.Parameters.AddWithValue("@FirstName", dto.FirstName.Trim());
                userCmd.Parameters.AddWithValue("@LastName", dto.LastName.Trim());
                userCmd.Parameters.AddWithValue("@Email", email);
                newUserId = Convert.ToInt32(await userCmd.ExecuteScalarAsync());
            }

            // 5. Insert UserToken
            using (var tokenCmd = new SqlCommand(@"
        INSERT INTO dbo.UserToken (UserId, Type, TokenHash)
        VALUES (@UserId, 'Activation', @TokenHash);", connection, transaction))
            {
                tokenCmd.Parameters.AddWithValue("@UserId", newUserId);
                tokenCmd.Parameters.AddWithValue("@TokenHash", tokenHash);
                await tokenCmd.ExecuteNonQueryAsync();
            }

            var baseUrl = _configuration["AppSettings:PortalBaseUrl"];
            return new UserCreatedDto
            {
                UserId = newUserId,
                FirstName = dto.FirstName.Trim(),
                LastName = dto.LastName.Trim(),
                Email = email,
                ActivationLink = $"{baseUrl}/activeer?token={rawToken}&email={Uri.EscapeDataString(dto.Email)}"
            };
        }

        public async Task DeleteUserAsync(int userId, int practitionerUserId)
        {
            using var connection = await _connectionFactory.GetAuthenticatedConnectionAsync(practitionerUserId);
            using var transaction = connection.BeginTransaction();
            try
            {
                using (var tokenCmd = new SqlCommand(
                    "DELETE FROM dbo.UserToken WHERE UserId = @UserId", connection, transaction))
                {
                    tokenCmd.Parameters.AddWithValue("@UserId", userId);
                    await tokenCmd.ExecuteNonQueryAsync();
                }

                using (var userCmd = new SqlCommand(
                    "DELETE FROM dbo.AppUser WHERE UserId = @UserId", connection, transaction))
                {
                    userCmd.Parameters.AddWithValue("@UserId", userId);
                    await userCmd.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}