using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Serilog;
using System.Threading.Tasks;

namespace ContactsManager.Middleware
{
    public class ExecptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExecptionHandlingMiddleware> _logger;
        private readonly IDiagnosticContext _diagnosticContext;

        public ExecptionHandlingMiddleware(RequestDelegate next, ILogger<ExecptionHandlingMiddleware> logger, IDiagnosticContext diagnosticContext)
        {
            _next = next;
            _logger = logger;
            _diagnosticContext = diagnosticContext;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                {

                    _logger.LogError("{ExecptionType}{ExecptionMessage}", ex.InnerException.GetType().ToString(), ex.InnerException.Message);
                }
                else
                {
                    _logger.LogError("{ExecptionType}{ExecptionMessage}", ex.GetType().ToString(), ex.Message);

                }
           //     httpContext.Response.StatusCode = 500;
               //await httpContext.Response.WriteAsync("Error Occured Please Try Again");
            }
        }
    }

    public static class ExecptionHandlingMiddlewareExtensions
    {
        public static IApplicationBuilder UseExecptionHandlingMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ExecptionHandlingMiddleware>();
        }
    }
}
