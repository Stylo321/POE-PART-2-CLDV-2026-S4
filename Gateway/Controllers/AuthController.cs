using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Azure;
using CoffeeNChill.Gateway.Models;
using CoffeeNChill.Gateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace CoffeeNChill.Gateway.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly UserProfileService _userProfileService;
        private readonly IConfiguration _configuration;

        public AuthController(
            UserProfileService userProfileService,
            IConfiguration configuration)
        {
            _userProfileService = userProfileService;
            _configuration = configuration;
        }

        /// <summary>
        /// Registers a new Customer account and stores its hashed password.
        /// </summary>
        /// <param name="request">The customer's name, email and password.</param>
        /// <returns>A confirmation that the account was created.</returns>
        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register(
            [FromBody] RegisterRequest request)
        {
            string email = request.Email.Trim().ToLowerInvariant();

            UserProfile? existingUser =
                await _userProfileService.FindByEmailAsync(email);

            if (existingUser != null)
            {
                return Conflict(new
                {
                    message = "An account with this email already exists."
                });
            }

            var user = new UserProfile
            {
                PartitionKey = "Customer",
                RowKey = email,
                FullName = request.FullName.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(
                    request.Password),
                CreatedDate = DateTime.UtcNow
            };

            try
            {
                await _userProfileService.CreateAsync(user);
            }
            catch (RequestFailedException ex) when (ex.Status == 409)
            {
                return Conflict(new
                {
                    message = "An account with this email already exists."
                });
            }

            return StatusCode(StatusCodes.Status201Created, new
            {
                message = "Account registered successfully.",
                email = user.RowKey,
                role = user.PartitionKey
            });
        }

        /// <summary>
        /// Checks a user's credentials and returns a signed JWT.
        /// </summary>
        /// <param name="request">The email address and password.</param>
        /// <returns>A Bearer token when the credentials are valid.</returns>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login(
            [FromBody] LoginRequest request)
        {
            string email = request.Email.Trim().ToLowerInvariant();

            UserProfile? user =
                await _userProfileService.FindByEmailAsync(email);

            if (user == null ||
                !BCrypt.Net.BCrypt.Verify(
                    request.Password,
                    user.PasswordHash))
            {
                return Unauthorized(new
                {
                    message = "Invalid email or password."
                });
            }

            string jwtKey = _configuration["Jwt:Key"]
                ?? throw new InvalidOperationException(
                    "JWT signing key is missing.");

            string issuer = _configuration["Jwt:Issuer"]
                ?? throw new InvalidOperationException(
                    "JWT issuer is missing.");

            string audience = _configuration["Jwt:Audience"]
                ?? throw new InvalidOperationException(
                    "JWT audience is missing.");

            int expirationMinutes =
                _configuration.GetValue<int>("Jwt:ExpiresMinutes", 60);

            DateTime expiresAt = DateTime.UtcNow.AddMinutes(
                expirationMinutes > 0 ? expirationMinutes : 60);

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.RowKey),
                new Claim(ClaimTypes.Role, user.PartitionKey),
                new Claim(ClaimTypes.NameIdentifier, user.RowKey),
                new Claim(
                    JwtRegisteredClaimNames.Jti,
                    Guid.NewGuid().ToString())
            };

            var signingKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey));

            var credentials = new SigningCredentials(
                signingKey,
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials);

            string tokenString =
                new JwtSecurityTokenHandler().WriteToken(token);

            return Ok(new
            {
                accessToken = tokenString,
                tokenType = "Bearer",
                expiresAtUtc = expiresAt,
                user = new
                {
                    fullName = user.FullName,
                    email = user.RowKey,
                    role = user.PartitionKey
                }
            });
        }
    }
}