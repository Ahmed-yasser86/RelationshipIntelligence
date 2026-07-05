using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.ComponentModel.DataAnnotations;

namespace ContactsManager.API.Filters
{

    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Filters;

    namespace ContactsManager.API.Filters
    {
        public class AccountsModelValidationActionFilter : IAsyncActionFilter
        {
            public async Task OnActionExecutionAsync(
                ActionExecutingContext context,
                ActionExecutionDelegate next)
            {
                if (!context.ModelState.IsValid)
                {

                    string errors = string.Join(", ", context.ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));

                    var problem = new ValidationProblemDetails(context.ModelState)
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "One or more validation errors occurred.",
                        Detail = errors,
                        Instance = context.HttpContext.Request.Path
                    };

                    context.Result = new BadRequestObjectResult(problem);
                    return;
                }

                await next();
            }
        }
    }
}

