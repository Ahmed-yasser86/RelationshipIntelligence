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
using ServiceContracts.DTOs;
using System.Security.Claims;

namespace ContactsManager.API.Controllers
{
    [ApiController]
    [AllowAnonymous]
    public class AccountController : CustomWebController
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IjwtAuthentication _jwtServices;

        public AccountController(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager, IjwtAuthentication JWTservices)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _jwtServices = JWTservices;
        }

        /* i added contacts here as a url bec 
         * in the Register DTO Through 
         * attribute validation we specified
         * the area as Contacts */
        [HttpGet]
        [Route("/Contacts/[controller]/[action]")]
        public async Task<IActionResult> VerifayUserByEmail(string email)
        {
            ApplicationUser user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return Ok(false);
            }
            else
            {
                return Ok(true);
            }
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

            if (user == null || !await _userManager.CheckPasswordAsync(user, loginDTO.Password))
            {
                return Unauthorized(new
                {
                    message = "Invalid email or password."
                });
            }

            var authenticationInformationObject = _jwtServices.Authenticate(user);

            user.RefreshToken = authenticationInformationObject.refreshToken;
            user.RefreshTokenExpirationTime = authenticationInformationObject.RefreshTokenExpirationTime;

            await _userManager.UpdateAsync(user);

            return Ok(authenticationInformationObject);
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
                if (registerDTO.UserType == UserTypeOptions.Admin)
                {
                    if (await _roleManager.FindByNameAsync(UserTypeOptions.Admin.ToString()) is null)
                    {
                        ApplicationRole applicationRole = new ApplicationRole() { Name = UserTypeOptions.Admin.ToString() };
                        await _roleManager.CreateAsync(applicationRole);
                    }

                    await _userManager.AddToRoleAsync(applicationUser, UserTypeOptions.Admin.ToString());
                }
                else
                {
                    if (await _roleManager.FindByNameAsync(UserTypeOptions.User.ToString()) is null)
                    {
                        ApplicationRole applicationRole = new ApplicationRole() { Name = UserTypeOptions.User.ToString() };
                        await _roleManager.CreateAsync(applicationRole);
                    }

                    await _userManager.AddToRoleAsync(applicationUser, UserTypeOptions.User.ToString());
                }

                var authenticationInformationObject = _jwtServices.Authenticate(applicationUser);
                applicationUser.RefreshToken = authenticationInformationObject.refreshToken;
                applicationUser.RefreshTokenExpirationTime = authenticationInformationObject.RefreshTokenExpirationTime;

                await _userManager.UpdateAsync(applicationUser);
                return Ok(authenticationInformationObject);
            }
            else
            {
                string errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return Problem(errors);
            }
        }
    }
}