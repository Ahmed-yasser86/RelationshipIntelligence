using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Servicess.Helpers;

namespace ContactsManager.API.Filters
{
    public sealed class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            var traceId = httpContext.TraceIdentifier;
            _logger.LogError(exception, "Unhandled exception on {Path} (trace {TraceId})",
                LogSanitizer.Sanitize(httpContext.Request.Path), traceId);

            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred",
                Instance = httpContext.Request.Path
            };
            problem.Extensions["traceId"] = traceId;

            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await Results.Problem(problem).ExecuteAsync(httpContext);
            return true;
        }
    }
}
