using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RepositryContracts;
using ServiceContracts;
using System;
using System.Threading.Tasks;

public class MeetingOwnershipFilter : IAsyncActionFilter
{
        private readonly MeetingRepositoryContract _meetings;
        private readonly ICurrentUserService _currentUserService;

        public MeetingOwnershipFilter(MeetingRepositoryContract meetings, ICurrentUserService currentUserService)
        {
            _meetings = meetings;
            _currentUserService = currentUserService;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (!context.ActionArguments.TryGetValue("id", out var idObj) || idObj is not Guid meetingId)
            {
                await next();
                return;
            }

            if (!_currentUserService.UserId.HasValue)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var meeting = await _meetings.GetAsync(_currentUserService.UserId.Value, meetingId);
            if (meeting == null)
            {
                context.Result = new ForbidResult();
                return;
            }

            context.HttpContext.Items["ValidatedMeeting"] = meeting;
            await next();
        }
    }
