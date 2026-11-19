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

            // 1. Checks if the email already exists
            var checkEmailQuery = "SELECT COUNT(1) FROM dbo.AppUser WHERE Email = @Email";
            using (var checkCmd = new SqlCommand(checkEmailQuery, connection))
            {
                checkCmd.Parameters.AddWithValue("@Email", dto.Email.Trim().ToLowerInvariant());
                int exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                if (exists > 0)
                    throw new InvalidOperationException("Er bestaat al een account met dit e-mailadres.");
            }

            // 2. Get roleid for the specified role name
            var roleQuery = "SELECT RoleId FROM dbo.Role WHERE Name = @RoleName";
            int roleId;
            using (var roleCmd = new SqlCommand(roleQuery, connection))
            {
                roleCmd.Parameters.AddWithValue("@RoleName", dto.RoleName);
                var result = await roleCmd.ExecuteScalarAsync();
                if (result == null)
                    throw new ArgumentException($"De rol '{dto.RoleName}' bestaat niet.");
                roleId = (int)result;
            }

            // 3. Generate activation token and hash it
            var rawToken = TokenHelper.GenerateRawToken();
            var tokenHash = TokenHelper.HashToken(rawToken);

            using var transaction = connection.BeginTransaction();
            try
            {
                
                // 4. Insert AppUser
                var insertUserQuery = @"
                    INSERT INTO dbo.AppUser (RoleId, FirstName, LastName, Email)
                    VALUES (@RoleId, @FirstName, @LastName, @Email);
                    SELECT SCOPE_IDENTITY();";

                int newUserId;
                using (var userCmd = new SqlCommand(insertUserQuery, connection, transaction))
                {
                    userCmd.Parameters.AddWithValue("@RoleId", roleId);
                    userCmd.Parameters.AddWithValue("@FirstName", dto.FirstName.Trim());
                    userCmd.Parameters.AddWithValue("@LastName", dto.LastName.Trim());
                    userCmd.Parameters.AddWithValue("@Email", dto.Email.Trim().ToLowerInvariant());
                    newUserId = Convert.ToInt32(await userCmd.ExecuteScalarAsync());
                }

                // 5. Save the activation token in UserToken table
                var insertTokenQuery = @"
                    INSERT INTO dbo.UserToken (UserId, Type, TokenHash)
                    VALUES (@UserId, 'Activation', @TokenHash);";

                using (var tokenCmd = new SqlCommand(insertTokenQuery, connection, transaction))
                {
                    tokenCmd.Parameters.AddWithValue("@UserId", newUserId);
                    tokenCmd.Parameters.AddWithValue("@TokenHash", tokenHash);
                    await tokenCmd.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();

                var baseUrl = _configuration["AppSettings:PortalBaseUrl"];
                var activationLink = $"{baseUrl}/activeer?token={rawToken}&email={Uri.EscapeDataString(dto.Email)}";

                return new UserCreatedDto
                {
                    UserId = newUserId,
                    FirstName = dto.FirstName.Trim(),
                    LastName = dto.LastName.Trim(),
                    Email = dto.Email.Trim().ToLowerInvariant(),
                    ActivationLink = activationLink
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        //public async Task<bool> ValidateActivationTokenAsync(string email, string token)
        //{
        //    var tokenHash = TokenHelper.HashToken(token);
        //    using var connection = await _connectionFactory.GetConnectionAsync();
        //    var query = @"
        //        SELECT ut.TokenHash
        //        FROM dbo.UserToken ut
        //        JOIN dbo.AppUser au ON ut.UserId = au.UserId
        //        WHERE au.Email = @Email AND ut.Type = 'Inactive'";

        //}
    }
}