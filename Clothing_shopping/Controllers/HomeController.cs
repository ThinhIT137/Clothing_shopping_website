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

        public async Task<IActionResult> Favorite()
        {
            var userIdstring = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdstring))
            {
                return RedirectToAction("Login", "User");
            }
            Guid.TryParse(userIdstring, out Guid userId);

            var favorite = await context.FavoriteItems.Where(f => f.UserId == userId)
                                                        .Include(pv => pv.ProductVariant).ThenInclude(p => p.Product)
                                                        .OrderByDescending(f => f.AddedAt)
                                                        .AsNoTracking()
                                                        .ToListAsync();
            return View(favorite);
        }

        public async Task<IActionResult> delete_favorite(int id)
        {
            Console.WriteLine(id);
            var favoriteItem = await context.FavoriteItems.FirstOrDefaultAsync(f => f.FavoriteItemsId == id);
            if (favoriteItem != null)
            {
                context.FavoriteItems.Remove(favoriteItem);
                await context.SaveChangesAsync();
            }
            return RedirectToAction("Favorite");
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
                ""Áo"": [1, 2, 3, 4, 5, 6],
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
                    ""Áo"": [1, 2, 3, 4, 5, 6],
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
            IQueryable<Product> query = context.Products.Include(p => p.ProductVariants)
                                        .Include(p => p.Category);
            switch (TargetGroup)
            {
                case 0: // Nam
                    query = query.Where(p => p.TargetGroup == 0 || p.TargetGroup == 10); // 10 là cho cả nam nữ
                    break;
                case 1: // Nu
                    query = query.Where(p => p.TargetGroup == 1 || p.TargetGroup == 10); // 10 là cho cả nam nữ
                    break;
                case 20:
                    query = query.Where(p => p.TargetGroup == 2 || p.TargetGroup == 20); // 20 là cho cả nam, 2 là cả nam nữ (trẻ em)
                    break;
                case 21:
                    query = query.Where(p => p.TargetGroup == 2 || p.TargetGroup == 21); // 21 là cho cả nữ, 2 là cả nam nữ (trẻ em)
                    break;
                case 2: // Tre Em
                    query = query.Where(p => p.TargetGroup == 2 || p.TargetGroup == 20 || p.TargetGroup == 21); // 20 là cho cả nam, 21 là cho cả nữ, 2 là cả nam nữ (trẻ em)
                    break;
            }
            // --- LOGIC LỌC SẢN PHẨM THEO GIÁ, MÀU SẮC, KÍCH CỠ KHI CÓ FILTER---
            if (minPrice.HasValue && maxPrice.HasValue)
            {
                query = query.Where(p => p.ProductVariants.Any(
                    pv => (pv.Price >= minPrice && pv.Price <= maxPrice)
                    || (pv.SalePrice >= minPrice && pv.SalePrice <= maxPrice)));
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
            // lấy color sản phẩm
            ViewBag.colorFilter = await context.ProductVariants.Select(c => c.Color).Distinct().ToListAsync();
            // lấy size sản phẩm
            ViewBag.sizeFilter = await context.ProductVariants.Select(c => c.Size).Distinct().ToListAsync();

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
            Product? product;

            if (TargetGroup == 0 || TargetGroup == 1)
            {
                product = await context.Products.Include(p => p.ProductVariants)
                                                .FirstOrDefaultAsync(p =>
                                                (p.TargetGroup == TargetGroup || p.TargetGroup == 10) &&
                                                p.CategoryId == CategoryId &&
                                                p.ProductId == ProductId);
            }
            else
            {
                product = await context.Products.Include(p => p.ProductVariants)
                                                .FirstOrDefaultAsync(p =>
                                                (p.TargetGroup == TargetGroup || p.TargetGroup == 2) &&
                                                p.CategoryId == CategoryId &&
                                                p.ProductId == ProductId);
            }

            var AllGroup = new Dictionary<int, string> {
                { 0, "Nam" },
                { 1, "Nữ" },
                { 2, "Trẻ em" },
                { 20, "Trẻ em nam" },
                { 21, "Trẻ em nữ" }
            };
            ViewBag.AllGroup = AllGroup;
            ViewBag.AllCategories = context.Categories.ToDictionary(c => c.CategoryId, c => c.Name);

            var reviews = await context.Reviews
                .Include(r => r.User) // Để lấy tên người đánh giá
                .Include(r => r.ProductVariant)
                .Where(r => r.ProductVariant.ProductId == ProductId && r.IsApproved)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            ViewBag.Reviews = reviews;

            // Tính điểm trung bình
            if (reviews.Any())
            {
                ViewBag.AverageRating = reviews.Average(r => r.Rating);
                ViewBag.TotalReviews = reviews.Count;
            }
            else
            {
                ViewBag.AverageRating = 0;
                ViewBag.TotalReviews = 0;
            }
            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> PostReview(int productId, byte rating, string content, string title)
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString))
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để đánh giá!" });
            }
            Guid userId = Guid.Parse(userIdString);

            // Tìm ProductVariant đầu tiên của Product này để gắn Review (Vì bảng Review của bạn link với ProductVariant)
            // Hoặc bạn có thể cho user chọn size/màu rồi mới đánh giá variant đó.
            // Ở đây mình lấy variant mặc định để đơn giản hóa.
            var variant = await context.ProductVariants
                .FirstOrDefaultAsync(pv => pv.ProductId == productId);

            if (variant == null)
            {
                return Json(new { success = false, message = "Sản phẩm không tồn tại!" });
            }

            // [Optional] Kiểm tra xem User đã mua hàng chưa (nếu muốn chặn spam)
            // bool hasPurchased = ...

            var review = new Review
            {
                ProductVariantId = variant.ProductVariantId,
                UserId = userId,
                Rating = rating,
                Title = title,
                Content = content,
                CreatedAt = DateTime.Now,
                IsApproved = true // Hoặc false nếu cần duyệt
            };

            context.Reviews.Add(review);
            await context.SaveChangesAsync();

            return Json(new { success = true, message = "Đánh giá thành công!" });
        }

        [HttpGet]
        public async Task<IActionResult> SearchProducts(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return Json(new { success = false, data = new List<object>() });
            }

            var products = await context.Products
                .Include(p => p.ProductVariants)
                .Where(p => p.Name.ToLower().Contains(keyword.ToLower()))
                .Take(5) // Chỉ lấy 5 sản phẩm đầu tiên
                .Select(p => new {
                    p.ProductId,
                    p.Name,
                    p.TargetGroup,
                    p.CategoryId,
                    // Lấy ảnh đầu tiên
                    Image = p.Images,
                    // Lấy giá thấp nhất
                    Price = p.ProductVariants.Any() ? p.ProductVariants.Min(v => v.Price) : 0
                })
                .ToListAsync();

            // Xử lý ảnh (Parse JSON)
            var result = products.Select(p => {
                string imgUrl = "/images/default.png";
                try
                {
                    var listImg = JsonConvert.DeserializeObject<List<string>>(p.Image);
                    if (listImg != null && listImg.Any()) imgUrl = listImg[0];
                }
                catch { }

                return new
                {
                    p.ProductId,
                    p.Name,
                    p.TargetGroup,
                    p.CategoryId,
                    Image = imgUrl,
                    Price = p.Price
                };
            });

            return Json(new { success = true, data = result });
        }
    }
}