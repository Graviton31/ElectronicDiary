using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicDiaryWeb.Controllers
{
    [Authorize]
    public class Users : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
