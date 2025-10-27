using Clothing_shopping.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Diagnostics;
using X.PagedList.Extensions;

namespace Clothing_shopping.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ClothingContext context;

        public HomeController(ILogger<HomeController> logger, ClothingContext _context)
        {
            _logger = logger;
            this.context = _context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Search()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public async Task<IActionResult> Product(int? Page, int? TargetGroup, int? CategoryId)
        {
            int pageSize = 10;
            int pageNumber = Page ?? 1;

            // Catagory nam
            string jsonStringNam = @"{
                ""Áo"": [1, 2, 3, 4, 5],
                ""Quần"": [7, 8, 9],
                ""Giày"": [12, 13],
                ""Dép"": [16, 17, 18]
            }";
            // Catagory Nữ
            string jsonStringNu = @"{
                ""Áo"": [1, 2, 3, 4, 5, 6],
                ""Quần"": [7, 8, 9],
                ""Váy"": [10, 11],
                ""Giày"": [12, 13, 14, 15],
                ""Dép"": [16, 17, 18]
            }";
            // Catagory trẻ em
            string jsonStringTreEm = @"{
                ""Nam"": {
                    ""Áo"": [1, 2, 3, 4, 5],
                    ""Quần"": [7, 8, 9],
                    ""Giày"": [12, 13],
                    ""Dép"": [16, 17, 18]
                },
                ""Nữ"": {
                    ""Áo"": [1, 2, 3, 4, 5, 6],
                    ""Quần"": [7, 8, 9],
                    ""Váy"": [10, 11],
                    ""Giày"": [12, 13, 14, 15],
                    ""Dép"": [16, 17, 18]
                }
            }";

            var query = context.Products.Include(p => p.ProductVariants).AsQueryable();

            if (TargetGroup.HasValue)
            {
                query = query.Where(p => p.TargetGroup == TargetGroup.Value);
                if (TargetGroup.Value == 0) ViewBag.JsonInt = JsonConvert.DeserializeObject<Dictionary<string, List<int>>>(jsonStringNam);
                else if (TargetGroup.Value == 1) ViewBag.JsonInt = JsonConvert.DeserializeObject<Dictionary<string, List<int>>>(jsonStringNu);
                else if (TargetGroup.Value == 2) ViewBag.JsonInt = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, List<int>>>>(jsonStringTreEm);
            }

            if (CategoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == CategoryId);
            }

            // làm thanh chỉ ở product
            ViewBag.TargetGroup = TargetGroup;

            ViewBag.CatagoryName = context.Categories.Where(c => c.CategoryId == CategoryId)
                                                     .Select(c => c.Name)
                                                     .FirstOrDefault() ?? "Tất cả sản phẩm";
            // Lấy tất cả tên
            ViewBag.AllCategories = context.Categories.ToDictionary(c => c.CategoryId, c => c.Name);

            // phân trang
            var products = query.OrderBy(p => p.ProductId).ToPagedList(pageNumber, pageSize);

            return View(products);
        }
    }
}
