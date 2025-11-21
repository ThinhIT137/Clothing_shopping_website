using Microsoft.AspNetCore.Mvc;

namespace Clothing_shopping.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Sales_Dashboard() // Doanh thu
        {
            return View();
        }

        public async Task<IActionResult> Inventory_Dashboard() // Tồn kho
        {
            return View();
        }
        public async Task<IActionResult> Supply_Chain()
        {
            return View();
        }
        public async Task<IActionResult> Marketing()
        {
            return View();
        }
        public async Task<IActionResult> Customer_Insights_Dashboard()
        {
            return View();
        }
        public async Task<IActionResult> KPI_Dashboard()
        {
            return View();
        }
    }
}
