using ContactsManager.API.Filters.ContactsManager.API.Filters;
using ContactsManger.Core.Domain.IdentityEntities;
using ContactsManger.Core.ServiceContracts;
using Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ServiceContracts;
using ServiceContracts.DTOs;

namespace ContactsManager.API.Controllers
{
    [ApiController]
    [TypeFilter(typeof(ModelValidationActionFilter))]
    public class ContactsController : CustomWebController
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IPersonGetterService _personGetterService;
        private readonly IPersonSearcherService _personSearcherService;

        public ContactsController(UserManager<ApplicationUser> userManager, IPersonGetterService personGetterService, IPersonSearcherService personSearcher)
        {
            _userManager = userManager;
            _personGetterService = personGetterService;
            _personSearcherService = personSearcher;
        }

        /// <summary>
        /// Retrieves a paginated list of contacts optimized for the Dashboard Data Grid.
        /// </summary>
        /// <remarks>
        /// **Dashboard Grid Endpoint**
        /// This endpoint is specifically designed to feed the main data grid on the admin dashboard. 
        /// 
        /// **Pagination Defaults:**
        /// - `pageNumber`: Defaults to 1 if not provided.
        /// - `pageSize`: Defaults to 10 if not provided.
        /// </remarks>
        /// <param name="pageNumber">The current page number (default: 1)</param>
        /// <param name="pageSize">The number of records per page (default: 10)</param>
        /// <returns>A paginated list of persons formatted for the dashboard grid.</returns>
        [HttpGet]
        public async Task<IActionResult> GetContactsGrid([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {

            // latency decreased from 30 sec to 711 ms
            var result = await _personGetterService.GetPersonsViewBatched(pageNumber, pageSize);
            return Ok(result);
        }

        /// <summary>
        /// Retrieves contacts using server-side pagination and optional filtering.
        /// </summary>
        ///
        /// <remarks>
        /// **IMPORTANT:**
        /// This endpoint never loads the entire contacts table into memory.
        /// Only the requested page of matching contacts is retrieved from the database,
        /// making this endpoint suitable for large datasets.
        /// 
        /// **Pagination examples:**
        /// - `pageNumber = 1` and `pageSize = 10` returns the first 10 matching contacts.
        /// - `pageNumber = 2` and `pageSize = 10` returns contacts 11–20 that match the search criteria.
        /// 
        /// **Example request:**
        /// ```
        /// GET /api/Contacts/GetContactsFilteredByBatches?pageNumber=1&pageSize=10&SearchBy=Name&QueryParamter=Ned
        /// ```
        /// </remarks>
        ///
        /// <param name="pageNumber">The page number to retrieve. The first page is 1.</param>
        /// <param name="pageSize">The maximum number of contacts returned per page.</param>
        /// <param name="SearchBy">
        /// Specifies which contact field should be searched.
        /// 
        /// **Valid values:**
        /// - `Name`
        /// - `email`
        /// - `phone`
        /// - `DateOfBirth`
        /// - `Circles`
        /// - `ContactItemRoles`
        /// - `SystemStatusTags`
        /// - `UserDefinedTags`
        /// 
        /// The latest supported values can also be retrieved from the `GET /api/Contacts/search-fields` endpoint.
        /// </param>
        /// <param name="QueryParamter">The value to search for within the selected field. Examples: Ahmed, Microsoft, META.</param>
        /// <returns>A paginated collection of contacts that match the specified search criteria.</returns>
        [HttpGet]
        public async Task<IActionResult> GetContactsFilteredByBatches(string? QueryParamter, string SearchBy, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _personSearcherService.SearchPersonsBy_Batched(QueryParamter, SearchBy, pageNumber, pageSize);
            return Ok(result);

        }



        [HttpGet]
        public IActionResult GetValidSearchFields()
        {
            var fields = new List<string>
                                   {
        nameof(PersonRespones.Name),
        nameof(PersonRespones.email),
        nameof(PersonRespones.phone),
        nameof(PersonRespones.DateOfBirth),
        nameof(Person.Circles),
        nameof(Person.ContactItemRoles),
        nameof(Person.SystemStatusTags),
        nameof(Person.UserDefinedTags)
                             };

            return Ok(fields);
        }



    }
}