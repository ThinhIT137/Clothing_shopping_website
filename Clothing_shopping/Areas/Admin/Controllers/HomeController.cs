using Microsoft.AspNetCore.Mvc;

namespace Clothing_shopping.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            var userIdString = HttpContext.Session.GetString("UserIdAdmin");
            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToAction("Login", "User", new { area = "Admin" });
            }

            return View();
        }

        public async Task<IActionResult> Sales_Dashboard() // Doanh thu
        {
            var userIdString = HttpContext.Session.GetString("UserIdAdmin");
            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToAction("Login", "User", new { area = "Admin" });
            }

            return View();
        }

        public async Task<IActionResult> Inventory_Dashboard() // Tồn kho
        {
            var userIdString = HttpContext.Session.GetString("UserIdAdmin");
            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToAction("Login", "User", new { area = "Admin" });
            }

            return View();
        }
        public async Task<IActionResult> Supply_Chain()
        {
            var userIdString = HttpContext.Session.GetString("UserIdAdmin");
            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToAction("Login", "User", new { area = "Admin" });
            }

            return View();
        }
        public async Task<IActionResult> Marketing()
        {
            var userIdString = HttpContext.Session.GetString("UserIdAdmin");
            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToAction("Login", "User", new { area = "Admin" });
            }

            return View();
        }
        public async Task<IActionResult> Customer_Insights_Dashboard()
        {
            var userIdString = HttpContext.Session.GetString("UserIdAdmin");
            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToAction("Login", "User", new { area = "Admin" });
            }

            return View();
        }
        public async Task<IActionResult> KPI_Dashboard()
        {
            var userIdString = HttpContext.Session.GetString("UserIdAdmin");
            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToAction("Login", "User", new { area = "Admin" });
            }

            return View();
        }
    }
}
