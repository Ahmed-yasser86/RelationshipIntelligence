using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace ContactsManager.Areas.Contacts.Filters.AuthFilters
{
    public class TokenAuthorizationFilter : IAuthorizationFilter
    {
        private readonly string _expectedToken;

        public TokenAuthorizationFilter(IConfiguration configuration)
        {
            _expectedToken = configuration["Auth:Key"] ?? string.Empty;
        }

        void IAuthorizationFilter.OnAuthorization(AuthorizationFilterContext context)
        {
            if (!context.HttpContext.Request.Cookies.TryGetValue("Auth-key", out var provided)
                || string.IsNullOrEmpty(_expectedToken)
                || !FixedTimeEquals(_expectedToken, provided))
            {
                context.Result = new StatusCodeResult(StatusCodes.Status404NotFound);
            }
        }

        private static bool FixedTimeEquals(string expected, string? provided)
        {
            if (provided == null)
                return false;
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expected),
                Encoding.UTF8.GetBytes(provided));
        }
    }
}
