using ContactsManager.API.Filters.ContactsManager.API.Filters;
using ContactsManger.Core.Domain.IdentityEntities;
using ContactsManger.Core.ServiceContracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ServiceContracts;

namespace ContactsManager.API.Controllers
{
    [ApiController]
    [TypeFilter(typeof(ModelValidationActionFilter))]
    public class ContactsController : CustomWebController
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IPersonGetterService _personGetterService;

        public ContactsController(UserManager<ApplicationUser> userManager, IPersonGetterService personGetterService)
        {
            _userManager = userManager;
            _personGetterService = personGetterService;
        }

        /// <summary>
        /// Contacts Data Grid(dashboard) API Endpoint
        /// </summary>
        /// <param name="pageNumber">Page number (default 1)</param>
        /// <param name="pageSize">Number of records per page (default 10)</param>
        /// <returns>Paged list of persons for the dashboard grid</returns>
        [HttpGet]
        public async Task<IActionResult> GetContactsGrid([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _personGetterService.GetPersonsViewBatched(pageNumber, pageSize);
            return Ok(result);
        }
    }
}