using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RepositryContracts;
using ServiceContracts;

/// <summary>
/// this action filter is used to validate the ownership of a person entity based on the current user.
/// its used with an action method that has a parameter named "id" of type Guid, which represents the person id.
/// so it works with action methods like GetPersonById(Guid id), UpdatePerson(Guid id, PersonUpdateRequest request), DeletePerson(Guid id) etc.
/// but never use it with actions like CreatePerson(PersonCreateRequest request) ,
/// GetAllPersons().......etc because there is no person id to validate ownership for a new person.
/// </summary>


public class PersonOwnershipFilter : IAsyncActionFilter
{
    private readonly PersonRepositryContract _personRepository;
    private readonly ICurrentUserService _currentUserService;

    public PersonOwnershipFilter(PersonRepositryContract personRepository, ICurrentUserService currentUserService)
    {
        _personRepository = personRepository;
        _currentUserService = currentUserService;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.ActionArguments.TryGetValue("id", out var idObj) || idObj is not Guid personId)
        {
            await next();
            return;
        }

        if (!_currentUserService.UserId.HasValue)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var person = await _personRepository.GetPersonById(personId);
        if (person == null || person.ApplicationUserId != _currentUserService.UserId.Value)
        {
            context.Result = new ForbidResult();
            return;
        }

        context.HttpContext.Items["ValidatedPerson"] = person;
        await next();
    }
}