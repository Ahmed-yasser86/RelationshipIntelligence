using ContactsManager.API.Filters;
using ContactsManager.API.Filters.ContactsManager.API.Filters;
using ContactsManger.Core.Domain.IdentityEntities;
using ContactsManger.Core.DTOs;
using ContactsManger.Core.DTOs.Enums;
using ContactsManger.Core.ServiceContracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using ServiceContracts.DTOs;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ContactsManager.API.Controllers
{
    [EnableRateLimiting("Auth")]
    [ApiController]
    [AllowAnonymous]
    public class AccountController : CustomWebController
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IjwtAuthentication _jwtServices;
        private readonly string _jwtIssuer;
        private readonly string _jwtAudience;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<ApplicationRole> roleManager,
            IjwtAuthentication JWTservices,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _jwtServices = JWTservices;
            _jwtIssuer = configuration["jwt:issuer"] ?? string.Empty;
            _jwtAudience = configuration["jwt:audience"] ?? string.Empty;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> PostLogout()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            if (email != null)
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user != null)
                {
                    user.RefreshToken = null;
                    user.RefreshTokenExpirationTime = null;
                    await _userManager.UpdateAsync(user);
                }
            }
            return NoContent();
        }
        [HttpPost]

        [TypeFilter(typeof(ModelValidationActionFilter))]
        public async Task<IActionResult> PostLogin(LoginDTO loginDTO)
        {
            var user = await _userManager.FindByEmailAsync(loginDTO.Email);
            if (user == null)
                return Unauthorized(new { message = "Invalid email or password." });

            var result = await _signInManager.CheckPasswordSignInAsync(
                user, loginDTO.Password ?? string.Empty, lockoutOnFailure: true);
            if (!result.Succeeded)
                return Unauthorized(new { message = "Invalid email or password." });

            return Ok(await IssueTokensAsync(user));
        }
        /// <summary>
        /// Registers a new user account and returns a JWT token.
        /// </summary>
        /// <remarks>
        /// IMPORTANT NOTE:
        /// This method implements JWT authentication and authorization for the user after registration. 
        /// The return object contains a JWT token used for authorization in future requests, 
        /// along with other user information needed by the client.
        /// 
        /// UserType Attribute:
        /// This is an enum carrying values (0, 1, 2, 3, 4), where each value represents 
        /// a specific user type (e.g., 0 = Admin, 1 = User).
        /// </remarks>
        /// <param name="registerDTO">The registration details.</param>
        /// <returns>An object containing the JWT token and user info.</returns>
        [HttpPost]
        [TypeFilter(typeof(ModelValidationActionFilter))]
        public async Task<IActionResult> PostRegister(RegisterDTO registerDTO)
        {

            ApplicationUser applicationUser = new ApplicationUser() { Email = registerDTO.Email, PhoneNumber = registerDTO.Phone, UserName = registerDTO.Email, PersonName = registerDTO.PersonName };

            IdentityResult result = await _userManager.CreateAsync(applicationUser, registerDTO.Password);
            if (result.Succeeded)
            {
                if (await _roleManager.FindByNameAsync(UserTypeOptions.User.ToString()) is null)
                {
                    await _roleManager.CreateAsync(new ApplicationRole { Name = UserTypeOptions.User.ToString() });
                }
                await _userManager.AddToRoleAsync(applicationUser, UserTypeOptions.User.ToString());

                return Ok(await IssueTokensAsync(applicationUser));
            }

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return BadRequest(new { message = errors });
        }

        [HttpPost]
        [AllowAnonymous]
        [TypeFilter(typeof(ModelValidationActionFilter))]
        public async Task<IActionResult> PostRefresh(RefreshRequest request)
        {
            // [ApiController] validates [Required] members before this action runs.
            var userId = GetUserIdFromExpiredToken(request!.Token);
            if (userId == null)
                return Unauthorized(new { message = "Invalid token." });

            var user = await _userManager.FindByIdAsync(userId.Value.ToString());
            if (user == null || user.RefreshToken == null || user.RefreshTokenExpirationTime == null ||
                user.RefreshTokenExpirationTime <= DateTime.UtcNow ||
                !FixedTimeEquals(Hash(request.RefreshToken), user.RefreshToken))
                return Unauthorized(new { message = "Invalid or expired refresh token." });

            return Ok(await IssueTokensAsync(user));
        }

        private async Task<AuthentocationRespones> IssueTokensAsync(ApplicationUser user)
        {
            var response = _jwtServices.Authenticate(user.Id, user.Email, user.PersonName);
            user.RefreshToken = Hash(response.refreshToken);
            user.RefreshTokenExpirationTime = response.RefreshTokenExpirationTime;
            await _userManager.UpdateAsync(user);
            return response;
        }

        private Guid? GetUserIdFromExpiredToken(string token)
        {
            var key = Environment.GetEnvironmentVariable("JWT_KEY");
            if (string.IsNullOrWhiteSpace(key))
                return null;

            var parameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = false,
                ValidIssuer = _jwtIssuer,
                ValidAudience = _jwtAudience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
            };
            try
            {
                var principal = new JwtSecurityTokenHandler().ValidateToken(token, parameters, out _);
                var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
                return Guid.TryParse(value, out var id) ? id : null;
            }
            catch (SecurityTokenException)
            {
                return null;
            }
        }

        private static string Hash(string value) =>
            Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

        private static bool FixedTimeEquals(string a, string b) =>
            CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
    }
}