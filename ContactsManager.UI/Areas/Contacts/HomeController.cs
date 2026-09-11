using Microsoft.AspNetCore.Mvc;

namespace ContactsManager.Areas.Contacts
{
    public class HomeController : Controller
    {
        [Route("Error")]
        public IActionResult Index()
        {
            return View();
        }

    }
}
