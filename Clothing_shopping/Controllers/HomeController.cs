using Clothing_shopping.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Security.Policy;
using System.Threading.Tasks;
using X.PagedList;
using X.PagedList.EF;
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

        /* --- YÊU THÍCH SẢN PHẨM --- */
        [HttpPost]
        public async Task<IActionResult> AddFavorite([FromQuery] int productVariantId)
        {
            var userIdstring = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdstring))
            {
                return Unauthorized(new { success = false, message = "Bạn chưa đăng nhập" }); // trả về 401 Unauthorized
            }
            Guid.TryParse(userIdstring, out Guid userId);
            if (await context.FavoriteItems.AnyAsync(x => x.ProductVariantId == productVariantId && x.UserId == userId))
            {
                return Conflict(new { success = false, message = "Sản phẩm đã có trong yêu thích." }); // trả về 409 Conflict
            }

            FavoriteItem f = new FavoriteItem
            {
                UserId = userId,
                ProductVariantId = productVariantId
            };
            await context.FavoriteItems.AddAsync(f);
            await context.SaveChangesAsync();
            return Ok(new { success = true, message = "Đã thêm vào yêu thích." }); // trả về 200 OK
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Search()
        {
            return View();
        }

        /* --- TRANG SẢN PHẨM HOME --- */
        public async Task<IActionResult> Home()
        {
            var product = new Dictionary<int, List<Product>>();
            var categories = await context.Categories.ToListAsync();
            var query = context.Products.Include(p => p.ProductVariants);
            foreach (var category in categories)
            {
                var ProductsInCategory = await query.Where(p => p.CategoryId == category.CategoryId)
                                                    .OrderByDescending(p => p.ProductId).Take(5).ToListAsync();
                if (ProductsInCategory.Any()) product[category.CategoryId] = ProductsInCategory;
            }
            return View(product);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        //public async Task<IActionResult> ProductList(int? Page, int TargetGroup)
        //{
        //    int pageSize = 10;
        //    int pageNumber = Page ?? 1;
        //    var ProductList = new Dictionary<int, IPagedList<Product>>();
        //    var categories = await context.Categories.ToListAsync();
        //    var query = context.Products.Include(p => p.ProductVariants)
        //                                .Include(p => p.Category)
        //                                .Where(p => p.TargetGroup == TargetGroup);
        //    foreach (var category in categories)
        //    {
        //        var productsInCategory = await query.Where(p => p.CategoryId == category.CategoryId)
        //                                            .OrderBy(p => p.ProductId)
        //                                            .ToPagedListAsync(pageNumber, pageSize);
        //        if (productsInCategory.Any()) ProductList[category.CategoryId] = productsInCategory;
        //    }


        //    return View(ProductList);
        //}

        /* --- TRANG HIỂN THỊ SẢN PHẨM --- */
        public async Task<IActionResult> Product(
            // page bình thường
            int? Page,
            int TargetGroup,
            int? CategoryId,
            int? pagedCatId,
            int? pagedCatPage,
            // FILTER
            decimal? minPrice,
            decimal? maxPrice,
            string[] color,
            string[] sizes
        )
        {
            int pageSize = 10; // số lượng sản phẩn trên 1 page
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

            if (TargetGroup == 0) ViewBag.JsonInt = JsonConvert.DeserializeObject<Dictionary<string, List<int>>>(jsonStringNam);
            else if (TargetGroup == 1) ViewBag.JsonInt = JsonConvert.DeserializeObject<Dictionary<string, List<int>>>(jsonStringNu);
            else if (TargetGroup == 2) ViewBag.JsonInt = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, List<int>>>>(jsonStringTreEm);

            var ProductList = new Dictionary<int, IPagedList<Product>>(); // key: CategoryId, value: danh sách sản phẩm phân trang
            var categories = await context.Categories.ToListAsync(); // lấy tất cả category
            var query = context.Products.Include(p => p.ProductVariants)
                                        .Include(p => p.Category)
                                        .Where(p => p.TargetGroup == TargetGroup);
            // --- LOGIC LỌC SẢN PHẨM THEO GIÁ, MÀU SẮC, KÍCH CỠ KHI CÓ FILTER---
            if (minPrice.HasValue)
            {
                query = query.Where(p => p.ProductVariants.Any(pv => pv.Price >= minPrice));
            }
            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.ProductVariants.Any(pv => pv.Price <= maxPrice));
            }
            if (color != null && color.Length > 0)
            {
                query = query.Where(p => p.ProductVariants.Any(pv => color.Contains(pv.Color)));
            }
            if (sizes != null && sizes.Length > 0)
            {
                query = query.Where(p => p.ProductVariants.Any(pv => sizes.Contains(pv.Size)));
            }

            // --- LOGIC PHÂN TRANG CHO TRANG "TẤT CẢ SẢN PHẨM" VÀ THEO CATEGORY ---
            if (!CategoryId.HasValue || CategoryId.Value == 0)
            {
                // === LOGIC CHO TRANG "TẤT CẢ SẢN PHẨM" ===
                foreach (var category in categories)
                {
                    // Mặc định tất cả là trang 1
                    int currentPageForCategory = 1;
                    // Nếu category đang lặp trùng với category được yêu cầu chuyển trang
                    if (pagedCatId.HasValue && pagedCatId.Value == category.CategoryId)
                    {
                        // thì lấy số trang từ URL
                        currentPageForCategory = pagedCatPage ?? 1;
                    }
                    var productsInCategory = await query.Where(p => p.CategoryId == category.CategoryId)
                                                        .OrderBy(p => p.ProductId)
                                                        .ToPagedListAsync(currentPageForCategory, pageSize);
                    if (productsInCategory.Any())
                    {
                        ProductList[category.CategoryId] = productsInCategory;
                    }
                }
            }
            else // --- LOGIC CHO TRANG THEO CATEGORY CỤ THỂ ---
            {
                foreach (var category in categories)
                {
                    var productsInCategory = await query.Where(p => p.CategoryId == CategoryId.Value)
                                                        .OrderBy(p => p.ProductId)
                                                        .ToPagedListAsync(pageNumber, pageSize);
                    if (productsInCategory.Any()) ProductList[CategoryId.Value] = productsInCategory;
                }
            }

            // làm thanh chỉ ở product
            ViewBag.TargetGroup = TargetGroup;
            ViewBag.CatagoryName = context.Categories.Where(c => c.CategoryId == CategoryId)
                                                     .Select(c => c.Name)
                                                     .FirstOrDefault() ?? "Tất cả sản phẩm";
            // Lấy tất cả tên
            ViewBag.AllCategories = context.Categories.ToDictionary(c => c.CategoryId, c => c.Name);
            // Lấy sản phẩm được yêu thích
            var favoriteVariantIds = new HashSet<int>();
            var userIdString = HttpContext.Session.GetString("UserId");
            if (Guid.TryParse(userIdString, out Guid userIdGuid))
            {
                favoriteVariantIds = await context.FavoriteItems
                    .Where(f => f.UserId == userIdGuid)
                    .Select(f => f.ProductVariantId)
                    .ToHashSetAsync();
            }
            ViewData["FavoriteIds"] = favoriteVariantIds;

            // khi ajax gọi load
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_ProductListContainer", ProductList);
            }

            return View(ProductList);
        }

        /* --- TRANG CHI TIẾT SẢN PHẨM --- */
        public async Task<IActionResult> ProductDetail(int? TargetGroup, int? CategoryId, int? ProductId)
        {
            var query = await context.Products.Include(p => p.ProductVariants)
                                        .FirstOrDefaultAsync(p => p.TargetGroup == TargetGroup &&
                                                                  p.CategoryId == CategoryId &&
                                                                  p.ProductId == ProductId);
            var AllGroup = new Dictionary<int, string> {
                { 0, "Nam" },
                { 1, "Nữ" },
                { 2, "Trẻ em" }
            };
            ViewBag.AllGroup = AllGroup;
            ViewBag.AllCategories = context.Categories.ToDictionary(c => c.CategoryId, c => c.Name);

            return View(query);
        }
    }
}