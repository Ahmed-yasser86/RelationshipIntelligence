using Entities;
using Microsoft.AspNetCore.Mvc.Filters;
using ServiceContracts.DTOs;
using System.Linq;

namespace ContactsManager.Areas.Contacts.Filters.ActionFilters
{
    public class PersonListActionFilter : IActionFilter
    {

        private readonly ILogger<PersonListActionFilter> _logger;
        public PersonListActionFilter(ILogger<PersonListActionFilter> logger)
        {
            _logger = logger;
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            _logger.LogInformation("Method of has been executed " + $"{nameof(PersonListActionFilter)}");


        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            _logger.LogInformation("{FilterName}.{MethodName} of has been terminated " ,nameof(PersonListActionFilter),nameof(OnActionExecuting));

            if (context.ActionArguments.ContainsKey("SearchBy"))
            {


                string SearchBy = Convert.ToString(context.ActionArguments["SearchBy"]);

                if (!string.IsNullOrEmpty(SearchBy)) {

                    var searchByOptions = new List<string>()
                    {
                        nameof(PersonRespones.Name),
                        nameof(PersonRespones.CountryId),
                        nameof(PersonRespones.DateOfBirth),
                        nameof(PersonRespones.Address),
                        nameof(PersonRespones.Gender),
                        nameof(PersonRespones.email)
                    };
                    if (searchByOptions.Any(temp=>temp==SearchBy)==false) {

                        _logger.LogInformation("searchBy Paramter acual value {searchBy}",SearchBy);

                        context.ActionArguments["SearchBy"]=nameof(PersonRespones.Name);

                        _logger.LogInformation("Search By has updated to", context.ActionArguments["SearchBy"]);
                    
                    }



                }

            }

        }
    }
}
