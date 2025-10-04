using Microsoft.AspNetCore.Mvc;

namespace Clothing_shopping.Controllers
{
    public class LoginController : Controller
    {
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(string email, string user, string password)
        {
            return View();
        }
    }
}
