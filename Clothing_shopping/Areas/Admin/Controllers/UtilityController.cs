using Microsoft.AspNetCore.Mvc;

namespace Clothing_shopping.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class UtilityController : Controller
    {
        public IActionResult DevFeature()
        {
            return View();
        }
    }
}
