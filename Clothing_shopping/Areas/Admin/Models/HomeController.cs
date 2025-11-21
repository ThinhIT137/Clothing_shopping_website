using Microsoft.AspNetCore.Mvc;

namespace Clothing_shopping.Areas.Admin.Models
{
    [Area("Admin")]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Login()
        {
            return View();
        }
    }
}
