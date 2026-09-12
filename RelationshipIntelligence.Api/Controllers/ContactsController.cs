using ContactsManager.API.Filters.ContactsManager.API.Filters;
using ContactsManger.Core.Domain.IdentityEntities;
using ContactsManger.Core.DTOs.PersonDTOs;
using ContactsManger.Core.ServiceContracts;
using Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ServiceContracts;
using ServiceContracts.DTOs;
using System.ComponentModel.DataAnnotations;

namespace ContactsManager.API.Controllers
{
    [ApiController]
    [TypeFilter(typeof(ModelValidationActionFilter))]
    public class ContactsController : CustomWebController
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IPersonGetterService _personGetterService;
        private readonly IPersonSearcherService _personSearcherService;
        private readonly ISystemTagsGetter _systemTagsGetter;
        private readonly IPersonQuickAdderService _personQuickAdderService;
        private readonly ICountryGetterService _countryGetterService;
        private readonly IPersonAdderService _personAdderService;
        private readonly IPersonUpdaterService _personUpdaterService;
        private readonly IPersonDeleterService _personDeleterService;
        private readonly IInteractionService _interactionService;
        private readonly IRelationshipScoringService _scoringService;
        private readonly IDemoWorkspaceService _demoWorkspaceService;


        public ContactsController(UserManager<ApplicationUser> userManager, IPersonGetterService personGetterService,
            IPersonSearcherService personSearcher, ISystemTagsGetter systemTagsGetter,
            IPersonQuickAdderService personQuickAdder, ICountryGetterService getCountries,
            IPersonAdderService PersoneAdderService, IPersonUpdaterService PersonesUpdater, IPersonDeleterService personDeleter,
            IInteractionService interactionService, IRelationshipScoringService scoringService,
            IDemoWorkspaceService demoWorkspaceService)
        {
            _userManager = userManager;
            _personGetterService = personGetterService;
            _personSearcherService = personSearcher;
            _systemTagsGetter = systemTagsGetter;
            _personQuickAdderService = personQuickAdder;
            _countryGetterService = getCountries;
            _personAdderService = PersoneAdderService;
            _personUpdaterService = PersonesUpdater;
            _personDeleterService = personDeleter;
            _interactionService = interactionService;
            _scoringService = scoringService;
            _demoWorkspaceService = demoWorkspaceService;
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
        /// Retrieves all predefined system status tags available in the application.
        /// </summary>
        /// <remarks>
        /// **System Lookup Endpoint**
        /// This endpoint fetches system-global tags (like "HighPriority", "Leads", etc.) used to categorize contacts.
        /// Commonly used to populate multi-select dropdowns or filter badges on the UI.
        /// 
        /// this has to be used with GetContactsFilteredByBatches() and  QueryContactsByCompositeFilter() only
        /// </remarks>
        /// <returns>A list of available system status tags.</returns>

        [HttpGet]
        public async Task<IActionResult> GetSystemStatusTags()
        {

            var systemtages = await _systemTagsGetter.GetSystemTags();


            return Ok(systemtages);

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
        ///
        /// 
        /// 
        /// 
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

        /// <summary>
        /// Retrieves contacts matching multiple filter conditions at once (e.g.
        /// Name + Tag, Phone + Tag), combined with server-side pagination.
        /// </summary>
        ///
        /// <remarks>
        /// **Composite Filtering**
        /// Unlike <c>GetContactsFilteredByBatches</c>, which searches exactly one
        /// field at a time, this endpoint accepts any combination of filter
        /// fields and ANDs them together. Only the fields you actually set are
        /// applied -- leave a field null/empty to skip it entirely.
        ///
        /// **Same pagination guarantee as the other endpoints:**
        /// only the requested page of matching contacts is loaded from the
        /// database; the full table is never pulled into memory.
        ///
        /// **Example request body:**
        /// ```json
        /// {
        ///   "name": "Ahmed",
        ///   "userDefinedTagName": "VIP"
        /// }
        /// ```
        /// This returns contacts whose Name contains "Ahmed" **and** who have a
        /// UserDefinedTag containing "VIP". Any field left null is ignored, so
        /// sending just <c>circleName</c> alone filters by Organization only.
        ///
        /// **Available filter fields (all optional, all substring-match):**
        /// - `name`
        /// - `email`
        /// - `phone`
        /// - `circleName` (Organization)
        /// - `contactItemRole` (Role)
        /// - `systemStatusTagName`
        /// - `userDefinedTagName`
        /// plz plz plz insure that other fields are empty and doesn't containe  any dumy value like (string,empty...etc)
        /// and only the fields that entred by user has value
        /// **Example request:**
        /// ```
        /// 
        /// {
        ///   "phone": "010",
        ///   "systemStatusTagName": "HighPriority"
        /// }
        /// ```
        /// </remarks>
        ///
        /// <param name="filter">The set of filter conditions to AND together. Any subset of fields may be provided.</param>
        /// <param name="pageNumber">The page number to retrieve. The first page is 1.</param>
        /// <param name="pageSize">The maximum number of contacts returned per page.</param>
        /// <returns>A paginated collection of contacts matching every filter condition supplied.</returns>
        [HttpPost]
        public async Task<IActionResult> QueryContactsByCompositeFilter(
            [FromBody] PersonCompositeFilter filter,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _personSearcherService.SearchPersonsByCompositeFilter(filter, pageNumber, pageSize);
            return Ok(result);
        }



        /// <summary>
        /// Retrieves the list of targetable field names supported by the single-field batch filter endpoint.
        /// </summary>
        /// <remarks>
        /// **Metadata Discovery**
        /// Use this endpoint dynamically on the client side to populate your "Search By" dropdown menus. 
        /// Any string returned by this list is guaranteed to be a valid argument for the <c>SearchBy</c> parameter 
        /// in the <c>GetContactsFilteredByBatches</c> action.
        /// </remarks>
        /// <returns>An array of valid property strings mapping to lookups on the Contact schema.</returns>
        [HttpGet]
        public IActionResult GetValidSearchFields()
        {
            var fields = new List<string>
                                   {
        nameof(Person.Name),
        nameof(Person.email),
        nameof(Person.phone),
        nameof(Person.Circles),
        nameof(Person.ContactItemRoles),
        nameof(Person.SystemStatusTags),
        nameof(Person.UserDefinedTags)
                             };

            return Ok(fields);
        }
        /// <summary>
        /// Retrieves the schema property names accepted by the Composite Filter payload.
        /// </summary>
        /// <remarks>
        /// **Metadata Discovery**
        /// Returns the exact, case-sensitive keys required when assembling a JSON body for 
        /// the <c>POST /api/Contacts/composite-filter</c> endpoint.
        /// </remarks>
        /// <returns>An array of valid property names representing the structure of a <see cref="PersonCompositeFilter"/>.</returns>
        [HttpGet]
        public IActionResult GetValidCompositeFilterFields()
        {
            var fields = new List<string>
            {
                nameof(PersonCompositeFilter.Name),
                nameof(PersonCompositeFilter.Email),
                nameof(PersonCompositeFilter.Phone),
                nameof(PersonCompositeFilter.CircleName),
                nameof(PersonCompositeFilter.ContactItemRole),
                nameof(PersonCompositeFilter.SystemStatusTagName),
                nameof(PersonCompositeFilter.UserDefinedTagName)
            };

            return Ok(fields);
        }



        /// <summary>
        /// Retrieves the list of field names supported by the sortBy parameter
        /// on GetPeople.
        /// </summary>
        /// <remarks>
        /// **Metadata Discovery**
        /// Use this endpoint dynamically on the client side to populate your "Sort By"
        /// dropdown menus. Any string returned by this list is guaranteed to be a
        /// valid argument for the <c>sortBy</c> parameter in the
        /// <c>GetPeople</c> action.
        ///
        /// Only fields SearchSortedPeopleBy actually has a case for are listed here --
        /// nameof(Person.Interactions) and nameof(Person.SystemStatusTags) map to
        /// special aggregate sort keys (last interaction date, most important tag
        /// respectively), and nameof(Person.Name) is the plain default/fallback sort.
        /// Any other string passed as sortBy silently falls back to sorting by Name,
        /// so this list intentionally does NOT advertise fields like email or phone
        /// until SearchSortedPeopleBy is extended to actually handle them -- keeping
        /// this list in sync with the switch avoids offering the frontend sort
        /// options that quietly do nothing.
        /// </remarks>
        /// <returns>An array of valid sortBy strings for GetSearchSortedPeopleBy.</returns>
        [HttpGet]
        public IActionResult GetValidSortFields()
        {
            var fields = new List<string>
    {
        nameof(Person.Name),
        nameof(Person.Interactions),
        nameof(Person.SystemStatusTags)
    };
            return Ok(fields);
        }


        /// <summary>
        /// Retrieves contacts sorted by complex dynamic keys with server-side pagination.
        /// </summary>
        /// <remarks>
        /// **Dynamic Server-Side Sorting**
        /// Pushes the sorting logic completely down to the database provider. Supports scalar fields as well as complex aggregate relationship fields (e.g., sorting by latest interaction).
        /// 
        /// **Sort Fallback:**
        /// If an unrecognized property is passed into the `sortBy` parameter, the database will silently fall back to sorting by `Name` to maintain contract stability.
        /// </remarks>
        /// <param name="page">The requested page number (default: 1).</param>
        /// <param name="size">The total number of entries per page slice (default: 10).</param>
        /// <param name="sortBy">
        /// The field key to sort the dataset by.
        /// 
        /// **Advertised Supported Keys:**
        /// - `Name` (Plain alphabetical sort)
        /// - `Interactions` (Maps to subquery sorting by the most recent interaction timestamp)
        /// - `SystemStatusTags` (Maps to subquery sorting by highest priority tag)
        /// </param>
        /// <returns>A sorted and sliced page result of contacts.</returns>

        [HttpGet]
        public async Task<IActionResult> GetSearchSortedPeopleBy([FromQuery] int page = 1, [FromQuery] int size = 10, [FromQuery] string sortBy = nameof(Person.Name))
        {
            var pagedResult = await _personSearcherService.SearchSortedPeopleBy(page, size, sortBy);

            return Ok(pagedResult);
        }


        [HttpGet]
        [ServiceFilter(typeof(PersonOwnershipFilter))]
        public async Task<IActionResult> GetContactByContactID(Guid? id)
        {
            PersonRespones? ContactObj = await _personGetterService.GetPersonByPersonId(id);

            if (ContactObj == null)
                return NotFound();

            return Ok(ContactObj);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="person"></param>
        /// <remarks>
        /// take care bec this return person respones object that has Many empty
        /// field as this request doesn't include all the information of the user
        /// </remarks>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> PostQuickAddContact([FromBody] PersonQuickAddRequest person)
        {
            if (_personQuickAdderService == null)
                throw new Exception("_personQuickAdderService is NULL");


            var PersonRespons = await _personQuickAdderService.QuickAddPerson(person);

            return Ok(PersonRespons);


        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="person"></param>
        /// <remarks>
        /// u could utlize this for retriving all countries in the data base and thier GUID 
        /// and use it for operations like persone update request ..etc 
        /// </remarks>
        /// <returns></returns>


        [HttpGet]
        public async Task<IActionResult> GetAllCountries()
        {

            var countries = await _countryGetterService.Countries();

            return Ok(countries);

        }

        [HttpPost]
        public async Task<IActionResult> PostAddPersoneRequest([FromBody] PersonAddRequest? PersoneAddRequest)
        {
            var PersonRespones = await _personAdderService.AddPerson(PersoneAddRequest);

            return Ok(PersonRespones);


        }

        [HttpPut]
        public async Task<IActionResult> PutContactItemUpdateRequest([FromBody] PersonUpdateRequest person)
        {
            try
            {
                var personResponesObject = await _personUpdaterService.UpdatePerson(person);
                return Ok(personResponesObject);
            }
            catch (ArgumentException)
            {
                return NotFound();
            }
        }

        [HttpPost]
        [ServiceFilter(typeof(PersonOwnershipFilter))]
        public async Task<IActionResult> DeletePersoneObject(Guid id)
        {
            var p = await _personDeleterService.DeletePersonByPersonId(id);

            if (!p)
                return NotFound();

            return Ok(p);
        }

        [HttpGet]
        [ServiceFilter(typeof(PersonOwnershipFilter))]
        public async Task<IActionResult> GetInteractionsForContact(Guid id)
        {
            var interactions = await _interactionService.ListForPersonAsync(id);

            return Ok(interactions);
        }

        [HttpGet]
        public async Task<IActionResult> GetRelationshipQueue([FromQuery] int top = 7)
        {
            var queue = await _scoringService.GetQueueAsync(top);

            return Ok(queue);
        }

        [HttpGet]
        [ServiceFilter(typeof(PersonOwnershipFilter))]
        public async Task<IActionResult> GetStateHistory(Guid id)
        {
            return Ok(await _scoringService.GetHistoryAsync(id));
        }

        [HttpPost]
        public async Task<IActionResult> PostSeedDemoWorkspace()
        {
            try
            {
                return Ok(await _demoWorkspaceService.SeedAsync());
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> PostClearDemoWorkspace()
        {
            return Ok(await _demoWorkspaceService.ClearAsync());
        }

        [HttpPost]
        public async Task<IActionResult> PostLogInteraction([FromBody] InteractionAddRequest request)
        {
            try
            {
                var response = await _interactionService.LogAsync(request);
                return Ok(response);
            }
            catch (ArgumentException)
            {
                return NotFound();
            }
            catch (ValidationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> PostImportInteractions(Guid id, [FromBody] CsvImportRequest request)
        {
            try
            {
                var count = await _interactionService.ImportCsvAsync(id, request?.CsvText);
                return Ok(count);
            }
            catch (ValidationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (ArgumentException)
            {
                return NotFound();
            }
        }

    }



}