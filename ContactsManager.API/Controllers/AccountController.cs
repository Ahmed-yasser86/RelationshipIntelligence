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
        public async Task<IActionResult> PostLogin(LoginDTO loginDTO)
        {
            if (!ModelState.IsValid)
            {
                string str = string.Join(", ", ModelState.Values.SelectMany(temp => temp.Errors).Select(temp => temp.ErrorMessage));
                return Problem(str);
            }

            ApplicationUser user = await _userManager.FindByEmailAsync(loginDTO.Email);

            if (user != null && await _userManager.CheckPasswordAsync(user, loginDTO.Password))
            {
                var authenticationInformationObject = _jwtServices.Authenticate(user);
                user.RefreshToken = authenticationInformationObject.refreshToken;
                user.RefreshTokenExpirationTime = authenticationInformationObject.RefreshTokenExpirationTime;
                await _userManager.UpdateAsync(user);

                return Ok(authenticationInformationObject);
            }

            return Problem("Invalid email or password.");
        }

        /// <summary>
        /// - IMPORTANT NOTE:
        /// this method implments JWT authentiction and authorization
        /// for the user after registration
        /// and the return object is an object that
        /// contains JWT token which is used for authorization in 
        /// the future requests with some other user
        /// information may be needed by the client side
        /// 
        /// - Another NOTE : UserType Attribute here is an enum which means it carries values like (0,1,2,3,4)
        /// each value represents a user type like (Admin, User, etc..)
        /// </summary>
        /// <param name="registerDTO"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> PostRegister(RegisterDTO registerDTO)
        {
            if (ModelState.IsValid == false)
            {
                string errors = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return Problem(errors);
            }

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