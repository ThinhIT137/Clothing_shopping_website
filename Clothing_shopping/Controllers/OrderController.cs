using Microsoft.AspNetCore.Mvc;

namespace Clothing_shopping.Controllers
{
    public class OrderController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
