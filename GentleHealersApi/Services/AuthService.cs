using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using GentleHealersApi.Services.Interfaces;

namespace GentleHealersApi.Services
{
    /// <summary>
    /// Provides authentication services, user verification via database, and JWT token generation.
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly string _connectionString;
        private readonly IConfiguration _configuration;
        /// <summary>
        /// Initializes a new instance of the <see cref="AuthService"/> class.
        /// </summary>
        /// <param name="configuration">The application configuration properties.</param>
        public AuthService(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }
        /// <summary>
        /// Authenticates a user based on their username and password, verifying credentials against the database and returning a JWT token if valid.
        /// </summary>
        /// <param name="email">The email of the user attempting to log in.</param>
        /// <param name="password">The plaintext password provided by the user.</param>
        /// <returns>A JWT token string if authentication succeeds; otherwise, null.</returns>
        public async Task<string?> AuthenticateUserAsync(string email, string password)
        {
            // 1. Verify user in database via Stored Procedure
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_VerifyUserLogin", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@Email", email);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                // Extract user details and the stored hash from the database result
                int userId = reader.GetInt32(reader.GetOrdinal("UserID"));
                string role = reader.GetString(reader.GetOrdinal("Role"));
                string storedHash = reader.GetString(reader.GetOrdinal("PasswordHash"));

                // 2. Verify the incoming password against the retrieved BCrypt hash
                bool isPasswordValid = BCrypt.Net.BCrypt.Verify(password, storedHash);
                if (!isPasswordValid)
                {
                    return null; // Password does not match
                }

                // 3. Generate JWT Token if valid
                return GenerateJwtToken(userId, email, role);
            }

            return null; // User not found
        }
        /// <summary>
        /// Generates a signed JWT token containing user identity claims and role assignments.
        /// </summary>
        /// <param name="userId">The unique ID of the user.</param>
        /// <param name="email">The email of the user.</param>
        /// <param name="role">The security role assigned to the user.</param>
        /// .<returns>The serialized JWT token string.</returns>
        private string GenerateJwtToken(int userId, string email, string role)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = Encoding.ASCII.GetBytes(jwtSettings["Key"]!);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Email, email),
                    new Claim(ClaimTypes.Role, role)
                }),
                Expires = DateTime.UtcNow.AddHours(3),
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}