using ContactsManager.ContactComponent.Controllers;
using ContactsManager.ContactComponent.Filters;
using ContactsManger.Core.Domain.IdentityEntities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using ServiceContracts.DTOs;
using System.ComponentModel.DataAnnotations;

namespace ContactsManager.UI.Areas.Contacts.Controllers.Account
{
    [Area("Contacts")]
    [TypeFilter(typeof(PerformanceLoggingFilter))]
    [TypeFilter(typeof(ExceptionFilter))]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public AccountController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }


        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> IndexAsync(RegisterDTO registerDTO)
        {

            if (!ModelState.IsValid)
            {
                ViewBag.Errors = ModelState.Values.SelectMany(x => x.Errors).
                    Select(x => x.ErrorMessage);


            }

            ApplicationUser applicationUser = new ApplicationUser() { Email = registerDTO.Email, PhoneNumber = registerDTO.Phone, UserName = registerDTO.Email, PersonName = registerDTO.PersonName };

            IdentityResult result = await _userManager.CreateAsync(applicationUser, registerDTO.Password);
            if (result.Succeeded)
            {
                return RedirectToAction(nameof(PersonController.Index), "Person");
            }
            else
            {
                foreach (IdentityError error in result.Errors)
                {
                    ModelState.AddModelError("Register", error.Description);
                }

                return View(registerDTO);

            }
        }

    }
}

