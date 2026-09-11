using Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SerilogTimings;
using ServiceContracts;
using ServiceContracts.DTOs;
using ServiceContracts.DTOs.Enums;
using ContactsManager.ContactComponent.Filters;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using ContactsManager.ContactComponent.Controllers;

namespace ContactsManager.UI.Areas.Contacts.Controllers
{
    [Area("Contacts")]
    [TypeFilter(typeof(PerformanceLoggingFilter))]
    [TypeFilter(typeof(ExceptionFilter))]
    public class PersonController : Controller
    {
        private readonly IPersonGetterService _personGetterService;
        private readonly IPersonAdderService _personAdderService;
        private readonly IPersonUpdaterService _personUpdaterService;
        private readonly IPersonDeleterService _personDeleterService;
        private readonly IPersonSearcherService _personSearcherService;
        private readonly IPersonSorterService _personSorterService;
        private readonly ICountryGetterService _countryGetterServices;

        public PersonController(
            IPersonGetterService personGetterService,
            IPersonAdderService personAdderService,
            IPersonUpdaterService personUpdaterService,
            IPersonDeleterService personDeleterService,
            IPersonSearcherService personSearcherService,
            IPersonSorterService personSorterService,
            ICountryGetterService countryGetterServices)
        {
            _personGetterService = personGetterService;
            _personAdderService = personAdderService;
            _personUpdaterService = personUpdaterService;
            _personDeleterService = personDeleterService;
            _personSearcherService = personSearcherService;
            _personSorterService = personSorterService;
            _countryGetterServices = countryGetterServices;
        }

        [Route("Person/Create")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            List<CountryResponse> countries = await _countryGetterServices.Countries();
            ViewBag.Countries = countries;
            return View();
        }

        [Route("Person/Create")]
        [HttpPost]
        public async Task<IActionResult> Create(PersonAddRequest model)
        {
            List<CountryResponse> countries = await _countryGetterServices.Countries();
            ViewBag.Countries = countries;

            if (ModelState.IsValid)
            {
                await _personAdderService.AddPerson(model);
                return RedirectToAction("Index", "Person");
            }

            var modelErrors = ModelState.Values.SelectMany(v => v.Errors);
            ViewBag.ModelErrors = modelErrors;
            return View(model);
        }

        [HttpGet]
        [Route("Person/Edit/{id}")]
        public async Task<IActionResult> Edit(Guid id)
        {
            List<CountryResponse> countries = await _countryGetterServices.Countries();
            ViewBag.Countries = countries;

            PersonRespones? person = await _personGetterService.GetPersonByPersonId(id);

            if (person == null)
            {
                return NotFound();
            }

            PersonUpdateRequest? personUpdate = person?.ToPersonUpdateRequest();
            return View(personUpdate);
        }

        [HttpPost]
        [Route("Person/Edit/{id}")]
        public async Task<IActionResult> Edit(PersonUpdateRequest model)
        {
            List<CountryResponse> countries = await _countryGetterServices.Countries();
            ViewBag.Countries = countries;

            if (ModelState.IsValid)
            {
                PersonRespones? updatedPerson = await _personUpdaterService.UpdatePerson(model);

                if (updatedPerson == null)
                {
                    return NotFound();
                }

                return RedirectToAction("Index", "Person");
            }

            var modelErrors = ModelState.Values.SelectMany(v => v.Errors);
            ViewBag.ModelErrors = modelErrors;
            return View(model);
        }

        [HttpGet]
        [Route("Person/Delete/{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var person = await _personGetterService.GetPersonByPersonId(id);

            if (person == null)
            {
                return RedirectToAction("Index", "Person");
            }

            return View(person);
        }

        [HttpPost]
        [Route("Person/Delete/{id}")]
        public async Task<IActionResult> Delete(PersonRespones model)
        {
            bool isDeleted = await _personDeleterService.DeletePersonByPersonId(model.PersonId);
            return RedirectToAction("Index", "Person");
        }

        [Route("Person/index")]
        [Route("/")]
        public async Task<IActionResult> Index(string SearchBy, string SearchString,
            string SortedBy, sortedListOp SortOption = sortedListOp.Ascending)
        {
            ViewBag.SearchFields = new Dictionary<string, string>()
        {
            {nameof(Person.email), "Email"},
            {nameof(Person.Gender), "Gender"},
            {nameof(Person.NewsLetter), "News Letter"},
            {nameof(Person.DateOfBirth), "Date Of Birth"},
            {nameof(Person.phone), "Phone"},
            {nameof(Person.CountryId), "Country"},
            {nameof(Person.Name), "Name"}
        };

            ViewBag.CurrentSearchBy = SearchBy;
            ViewBag.CurrentSearchString = SearchString;
            ViewBag.CurrentSortedBy = SortedBy;
            ViewBag.CurrentSortOption = SortOption.ToString();

            List<PersonRespones> persons = await _personSearcherService.SearchPersonsBy(SearchString, SearchBy);
            List<PersonRespones> SortedPersons = await _personSorterService.getPersonsSorted(persons, SortedBy, SortOption);

            return View(SortedPersons);
        }
    }
}