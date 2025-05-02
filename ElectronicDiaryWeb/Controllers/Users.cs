using Microsoft.AspNetCore.Mvc;

namespace ElectronicDiaryWeb.Controllers
{
    public class Users : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
