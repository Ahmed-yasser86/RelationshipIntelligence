using Microsoft.AspNetCore.Mvc.Filters;
using SerilogTimings;


namespace ContactsManager.ContactComponent.Filters
{
    public class PerformanceLoggingFilter : IAsyncActionFilter
    {
        private readonly ILogger<PerformanceLoggingFilter> _logger;

        public PerformanceLoggingFilter(ILogger<PerformanceLoggingFilter> logger)
        {
            _logger = logger;
        }

        public async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            var actionName = context.ActionDescriptor.RouteValues["action"];
            var controllerName = context.ActionDescriptor.RouteValues["controller"];

            using (Operation.Time($"{controllerName}.{actionName} execution"))
            {
                await next();
            }
        }
    }
}