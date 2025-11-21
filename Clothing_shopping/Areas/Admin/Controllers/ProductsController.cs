using Clothing_shopping.Areas.Admin.Models;
using Clothing_shopping.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using X.PagedList.EF; // Hoặc namespace X.PagedList tương ứng

namespace Clothing_shopping.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductsController : Controller
    {
        private readonly ClothingContext db;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductsController(ClothingContext db, IWebHostEnvironment webHostEnvironment)
        {
            this.db = db;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpGet]
        public IActionResult add_Products()
        {
            // 1. Load danh mục
            ViewBag.CategoryId = new SelectList(db.Categories, "CategoryId", "Name");

            // 2. Load đối tượng
            var targetGroups = new List<object>() {
                new { Id = 0, Name = "Nam" },
                new { Id = 1, Name = "Nữ" },
                new { Id = 20, Name = "Bé Trai" },
                new { Id = 21, Name = "Bé Gái" }
            };
            ViewBag.TargetGroup = new SelectList(targetGroups, "Id", "Name");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> add_Products(Product pro, List<ProductVariantInput> Variants, IFormFile? MainImage)
        {
            ModelState.Remove("Category");
            ModelState.Remove("Images");     // Vì ta tự tạo JSON ảnh sau
            ModelState.Remove("Slug");       // Vì ta tự tạo Slug từ tên
            ModelState.Remove("CreatedAt");  // Vì ta tự gán DateTime.Now
            ModelState.Remove("UpdatedAt");  // (Nếu có)
            ModelState.Remove("DeletedAt");  // (Nếu có)

            if (ModelState.IsValid)
            {
                // 1. SETUP CƠ BẢN
                pro.Slug = ToUrlSlug(pro.Name).Replace("_", "-");
                pro.CreatedAt = DateTime.Now;
                pro.IsHidden = false;

                string genderFolder = GetGenderFolder(pro.TargetGroup ?? 0);
                var category = await db.Categories.FindAsync(pro.CategoryId);
                string categoryFolder = category != null ? ToUrlSlug(category.Name) : "other";
                string productFolder = ToUrlSlug(pro.Name);

                string relativePath = Path.Combine("images", "Products", genderFolder, categoryFolder, productFolder);
                string absolutePath = Path.Combine(_webHostEnvironment.WebRootPath, relativePath);

                if (!Directory.Exists(absolutePath)) Directory.CreateDirectory(absolutePath);

                // --- LIST CHỨA TẤT CẢ ẢNH (MAIN + VARIANT) ---
                List<string> allImagesList = new List<string>();

                // 2. XỬ LÝ ẢNH ĐẠI DIỆN (MAIN IMAGE)
                if (MainImage != null && MainImage.Length > 0)
                {
                    string ext = Path.GetExtension(MainImage.FileName);
                    string mainFileName = $"{pro.Slug}{ext}";
                    string fullPath = Path.Combine(absolutePath, mainFileName);

                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        await MainImage.CopyToAsync(stream);
                    }
                    allImagesList.Add("/" + relativePath.Replace("\\", "/") + "/" + mainFileName);
                }

                // 3. LƯU PRODUCT TRƯỚC (Để lấy ID)
                pro.Images = "[]";
                if (string.IsNullOrEmpty(pro.ShortDesc)) pro.ShortDesc = "[]";
                if (string.IsNullOrEmpty(pro.FullDesc)) pro.FullDesc = "[]";

                db.Add(pro);
                await db.SaveChangesAsync();

                // 4. XỬ LÝ VARIANTS
                if (Variants != null)
                {
                    int variantIndex = 1;
                    foreach (var vInput in Variants)
                    {
                        var variant = new ProductVariant
                        {
                            ProductId = pro.ProductId,
                            Size = vInput.Size,
                            Color = vInput.Color,
                            Price = vInput.Price,
                            SalePrice = vInput.SalePrice,
                            Stock = vInput.Stock,
                            IsHidden = false,
                            CreatedAt = DateTime.Now
                        };

                        // Xử lý ảnh variant
                        if (vInput.ImageFiles != null && vInput.ImageFiles.Count > 0)
                        {
                            int imgSubIndex = 1;

                            foreach (var file in vInput.ImageFiles)
                            {
                                if (file.Length > 0)
                                {
                                    string ext = Path.GetExtension(file.FileName);
                                    string colorSlug = ToUrlSlug(vInput.Color);

                                    // Tên file: Ten_1_Red_1.png, Ten_1_Red_2.png
                                    string varFileName = $"{pro.Slug}_{variantIndex}_{imgSubIndex}_{colorSlug}{ext}";
                                    string fullPath = Path.Combine(absolutePath, varFileName);

                                    // Check trùng tên
                                    if (System.IO.File.Exists(fullPath))
                                    {
                                        varFileName = $"{pro.Slug}_{variantIndex}_{imgSubIndex}_{colorSlug}_{Guid.NewGuid().ToString().Substring(0, 3)}{ext}";
                                        fullPath = Path.Combine(absolutePath, varFileName);
                                    }

                                    using (var stream = new FileStream(fullPath, FileMode.Create))
                                    {
                                        await file.CopyToAsync(stream);
                                    }

                                    string dbPath = "/" + relativePath.Replace("\\", "/") + "/" + varFileName;

                                    // A. Thêm TẤT CẢ ảnh vào List tổng JSON của Product
                                    allImagesList.Add(dbPath);

                                    // B. (Tùy chọn) Lưu ảnh đầu tiên làm đại diện cho Variant trong bảng ProductVariant
                                    // Vì bảng ProductVariant cột Image là string (chỉ lưu 1 đường dẫn)
                                    /* if (imgSubIndex == 1) 
                                    {
                                        variant.Image = dbPath; 
                                    }
                                    */

                                    imgSubIndex++;
                                }
                            }
                        }

                        db.Add(variant);
                        variantIndex++;
                    }
                    await db.SaveChangesAsync();
                }

                // 5. UPDATE JSON ẢNH VÀO PRODUCT
                pro.Images = JsonConvert.SerializeObject(allImagesList);
                db.Update(pro);
                await db.SaveChangesAsync();

                return RedirectToAction(nameof(ListProduct));
            }

            // Reload View nếu lỗi
            ViewBag.CategoryId = new SelectList(db.Categories, "CategoryId", "Name", pro.CategoryId);
            var tGroups = new List<object>() {
                new { Id = 0, Name = "Nam" },
                new { Id = 1, Name = "Nữ" },
                new { Id = 20, Name = "Bé Trai" },
                new { Id = 21, Name = "Bé Gái" }
            };
            ViewBag.TargetGroup = new SelectList(tGroups, "Id", "Name", pro.TargetGroup);

            return View(pro);
        }

        // GET: Products/ListProduct
        public async Task<IActionResult> ListProduct(int? Page,
            string? name,
            string? CategoryIds,
            decimal? minPrice,
            decimal? maxPrice,
            string[] color,
            string[] sizes,
            int? TargetGroup = 0 // Mặc định là 0 (Nam)
        )
        {
            int pageSize = 10;
            int pageNumber = Page ?? 1;
            IQueryable<Product> query = db.Products.Include(p => p.ProductVariants).AsNoTracking();

            // 1. Lọc TargetGroup
            if (TargetGroup.HasValue)
            {
                switch (TargetGroup)
                {
                    case 0: // Nam
                        query = query.Where(p => p.TargetGroup == 0 || p.TargetGroup == 10);
                        break;
                    case 1: // Nữ
                        query = query.Where(p => p.TargetGroup == 1 || p.TargetGroup == 10);
                        break;
                    case 20: // Bé trai
                        query = query.Where(p => p.TargetGroup == 2 || p.TargetGroup == 20);
                        break;
                    case 21: // Bé gái
                        query = query.Where(p => p.TargetGroup == 2 || p.TargetGroup == 21);
                        break;
                    case 2: // Trẻ em chung
                        query = query.Where(p => p.TargetGroup == 2 || p.TargetGroup == 20 || p.TargetGroup == 21);
                        break;
                }
            }

            // 2. Xử lý JSON Menu (ĐOẠN NÀY LÚC NÃY BẠN THIẾU)
            string jsonStringNam = @"{""Áo"": [1, 2, 3, 4, 5, 6], ""Quần"": [7, 8, 9], ""Giày"": [12, 13], ""Dép"": [16, 17, 18]}";
            string jsonStringNu = @"{""Áo"": [1, 2, 3, 4, 5, 6], ""Quần"": [7, 8, 9], ""Váy"": [10, 11], ""Giày"": [12, 13, 14, 15], ""Dép"": [16, 17, 18]}";
            string jsonStringTreEm = @"{""Nam"": {""Áo"": [1, 2, 3, 4, 5, 6], ""Quần"": [7, 8, 9], ""Giày"": [12, 13], ""Dép"": [16, 17, 18]}, ""Nữ"": {""Áo"": [1, 2, 3, 4, 5, 6], ""Quần"": [7, 8, 9], ""Váy"": [10, 11], ""Giày"": [12, 13, 14, 15], ""Dép"": [16, 17, 18]}}";

            Dictionary<string, string> menuCategories = new Dictionary<string, string>();

            try
            {
                if (TargetGroup == 0) // Nam
                {
                    var jsonObj = JsonConvert.DeserializeObject<Dictionary<string, List<int>>>(jsonStringNam);
                    foreach (var item in jsonObj) menuCategories.Add(item.Key, string.Join(",", item.Value));
                }
                else if (TargetGroup == 1) // Nữ
                {
                    var jsonObj = JsonConvert.DeserializeObject<Dictionary<string, List<int>>>(jsonStringNu);
                    foreach (var item in jsonObj) menuCategories.Add(item.Key, string.Join(",", item.Value));
                }
                else // Trẻ em
                {
                    var rootObj = JObject.Parse(jsonStringTreEm);
                    JToken targetObj = null;

                    if (TargetGroup == 20) targetObj = rootObj["Nam"];
                    else if (TargetGroup == 21) targetObj = rootObj["Nữ"];
                    else targetObj = rootObj["Nam"]; // Mặc định

                    if (targetObj != null)
                    {
                        var dict = targetObj.ToObject<Dictionary<string, List<int>>>();
                        foreach (var item in dict) menuCategories.Add(item.Key, string.Join(",", item.Value));
                    }
                }
            }
            catch { }

            ViewBag.MenuCategories = menuCategories;

            // 3. Các bộ lọc khác
            if (!string.IsNullOrEmpty(name))
            {
                var searchTerm = name.Trim().ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(searchTerm));
            }
            if (!string.IsNullOrEmpty(CategoryIds))
            {
                try
                {
                    // Thêm Trim() để tránh lỗi nếu có khoảng trắng thừa
                    var catIdList = CategoryIds.Split(',')
                                               .Where(x => !string.IsNullOrEmpty(x)) // Bỏ các phần tử rỗng
                                               .Select(int.Parse)
                                               .ToList();

                    if (catIdList.Any()) // Chỉ lọc nếu list có ID
                    {
                        query = query.Where(p => catIdList.Contains(p.CategoryId));
                    }
                }
                catch { }
            }
            if (minPrice.HasValue && maxPrice.HasValue)
            {
                query = query.Where(p => p.ProductVariants.Any(pv =>
                    (pv.SalePrice ?? pv.Price) >= minPrice && (pv.SalePrice ?? pv.Price) <= maxPrice
                ));
            }
            if (color != null && color.Length > 0)
            {
                query = query.Where(p => p.ProductVariants.Any(pv => color.Contains(pv.Color)));
            }
            if (sizes != null && sizes.Length > 0)
            {
                query = query.Where(p => p.ProductVariants.Any(pv => sizes.Contains(pv.Size)));
            }

            var products = await query.OrderBy(p => p.ProductId).ToPagedListAsync(pageNumber, pageSize);

            // 4. Trả về ViewBag
            ViewBag.CurrentTargetGroup = TargetGroup;
            ViewBag.CurrentName = name;
            ViewBag.CurrentCategoryIds = CategoryIds;
            ViewBag.CurrentMinPrice = minPrice;
            ViewBag.CurrentMaxPrice = maxPrice;
            ViewBag.CurrentColors = color;
            ViewBag.CurrentSizes = sizes;

            // 1.Lấy tất cả các màu có trong bảng ProductVariants(loại bỏ trùng lặp)
            ViewBag.AllColors = await db.ProductVariants
                            .Where(x => !string.IsNullOrEmpty(x.Color)) // Bỏ màu rỗng
                            .Select(x => x.Color)
                            .Distinct() // Lấy duy nhất
                            .OrderBy(x => x) // Sắp xếp A-Z
                            .ToListAsync();

            // 2. Lấy tất cả các size có trong bảng ProductVariants
            ViewBag.AllSizes = await db.ProductVariants
                                       .Where(x => !string.IsNullOrEmpty(x.Size))
                                       .Select(x => x.Size)
                                       .Distinct()
                                       .ToListAsync();

            // Sắp xếp Size theo thứ tự logic (S, M, L, XL...) thay vì Alpha B -> Cần logic riêng
            // Nhưng tạm thời để nguyên hoặc sắp xếp A-Z
            ViewBag.AllSizes = ((List<string>)ViewBag.AllSizes).OrderBy(x => x).ToList();


            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("~/Areas/Admin/Views/Shared/_ProductList.cshtml", products);
            }

            return View(products);
        }

        private string GetGenderFolder(int targetId)
        {
            return targetId switch { 0 => "Nam", 1 => "Nu", _ => "TreEm" };
        }

        // Hàm loại bỏ dấu Tiếng Việt và ký tự đặc biệt
        private string ToUrlSlug(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            text = text.ToLower().Trim();
            string[] arr1 = new string[] { "á", "à", "ả", "ã", "ạ", "â", "ấ", "ầ", "ẩ", "ẫ", "ậ", "ă", "ắ", "ằ", "ẳ", "ẵ", "ặ", "đ", "é", "è", "ẻ", "ẽ", "ẹ", "ê", "ế", "ề", "ể", "ễ", "ệ", "í", "ì", "ỉ", "ĩ", "ị", "ó", "ò", "ỏ", "õ", "ọ", "ô", "ố", "ồ", "ổ", "ỗ", "ộ", "ơ", "ớ", "ờ", "ở", "ỡ", "ợ", "ú", "ù", "ủ", "ũ", "ụ", "ư", "ứ", "ừ", "ử", "ữ", "ự", "ý", "ỳ", "ỷ", "ỹ", "ỵ" };
            string[] arr2 = new string[] { "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "d", "e", "e", "e", "e", "e", "e", "e", "e", "e", "e", "e", "i", "i", "i", "i", "i", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "u", "u", "u", "u", "u", "u", "u", "u", "u", "u", "u", "y", "y", "y", "y", "y" };
            for (int i = 0; i < arr1.Length; i++)
            {
                text = text.Replace(arr1[i], arr2[i]);
                text = text.Replace(arr1[i].ToUpper(), arr2[i].ToUpper());
            }
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", "_");
            text = System.Text.RegularExpressions.Regex.Replace(text, @"[^a-z0-9_]", "");
            return text;
        }

        public async Task<IActionResult> ListProductVariant(
            int? ProductId,
            int? Page,
            string? name,
            string? CategoryIds,
            decimal? minPrice,
            decimal? maxPrice,
            string[] color,
            string[] sizes,
            int? TargetGroup = 0 // Mặc định là 0 (Nam)
        )
        {
            int pageSize = 10;
            int pageNumber = Page ?? 1;

            // 1. THAY ĐỔI QUAN TRỌNG NHẤT: Query từ bảng ProductVariant
            // Include(v => v.Product) để lấy được Tên sản phẩm, Danh mục... từ bảng cha
            IQueryable<ProductVariant> query = db.ProductVariants
                                                 .Include(v => v.Product)
                                                 .AsNoTracking();

            if (ProductId.HasValue)
            {
                query = query.Where(v => v.ProductId == ProductId);
                ViewBag.CurrentProductId = ProductId;
            }

            // 2. Lọc TargetGroup (Truy cập qua v.Product)
            if (TargetGroup.HasValue)
            {
                switch (TargetGroup)
                {
                    case 0: // Nam
                        query = query.Where(v => v.Product.TargetGroup == 0 || v.Product.TargetGroup == 10);
                        break;
                    case 1: // Nữ
                        query = query.Where(v => v.Product.TargetGroup == 1 || v.Product.TargetGroup == 10);
                        break;
                    case 20: // Bé trai
                        query = query.Where(v => v.Product.TargetGroup == 2 || v.Product.TargetGroup == 20);
                        break;
                    case 21: // Bé gái
                        query = query.Where(v => v.Product.TargetGroup == 2 || v.Product.TargetGroup == 21);
                        break;
                    case 2: // Trẻ em chung
                        query = query.Where(v => v.Product.TargetGroup == 2 || v.Product.TargetGroup == 20 || v.Product.TargetGroup == 21);
                        break;
                }
            }

            // ... (ĐOẠN XỬ LÝ JSON MENU GIỮ NGUYÊN KHÔNG ĐỔI) ...
            // (Tôi ẩn đi cho gọn, bạn cứ copy y nguyên đoạn tạo Dictionary menuCategories vào đây)
            Dictionary<string, string> menuCategories = new Dictionary<string, string>();
            // ... Code xử lý JSON Menu cũ ...
            ViewBag.MenuCategories = menuCategories;


            // 3. Các bộ lọc khác

            // Lọc theo Tên (Truy cập qua bảng cha Product)
            if (!string.IsNullOrEmpty(name))
            {
                var searchTerm = name.Trim().ToLower();
                query = query.Where(v => v.Product.Name.ToLower().Contains(searchTerm));
            }

            // Lọc theo Danh mục (Truy cập qua bảng cha Product)
            if (!string.IsNullOrEmpty(CategoryIds))
            {
                try
                {
                    var catIdList = CategoryIds.Split(',')
                                               .Where(x => !string.IsNullOrEmpty(x))
                                               .Select(int.Parse)
                                               .ToList();

                    if (catIdList.Any())
                    {
                        query = query.Where(v => catIdList.Contains(v.Product.CategoryId));
                    }
                }
                catch { }
            }

            // Lọc theo Giá (Lọc trực tiếp trên Variant)
            if (minPrice.HasValue && maxPrice.HasValue)
            {
                // Lấy SalePrice nếu có, không thì lấy Price
                query = query.Where(v => (v.SalePrice ?? v.Price) >= minPrice && (v.SalePrice ?? v.Price) <= maxPrice);
            }

            // Lọc theo Màu (Lọc trực tiếp)
            if (color != null && color.Length > 0)
            {
                query = query.Where(v => color.Contains(v.Color));
            }

            // Lọc theo Size (Lọc trực tiếp)
            if (sizes != null && sizes.Length > 0)
            {
                query = query.Where(v => sizes.Contains(v.Size));
            }

            // Sắp xếp theo ID sản phẩm rồi đến ID biến thể để gom nhóm đẹp hơn
            var productVariants = await query.OrderBy(v => v.ProductId)
                                             .ThenBy(v => v.Size)
                                             .ToPagedListAsync(pageNumber, pageSize);

            // 4. Trả về ViewBag (Giữ nguyên)
            ViewBag.CurrentTargetGroup = TargetGroup;
            ViewBag.CurrentName = name;
            ViewBag.CurrentCategoryIds = CategoryIds;
            ViewBag.CurrentMinPrice = minPrice;
            ViewBag.CurrentMaxPrice = maxPrice;
            ViewBag.CurrentColors = color;
            ViewBag.CurrentSizes = sizes;

            // ViewBag cho Dropdown filter (Giữ nguyên)
            ViewBag.AllColors = await db.ProductVariants.Where(x => !string.IsNullOrEmpty(x.Color)).Select(x => x.Color).Distinct().OrderBy(x => x).ToListAsync();
            var sizesList = await db.ProductVariants.Where(x => !string.IsNullOrEmpty(x.Size)).Select(x => x.Size).Distinct().ToListAsync();
            ViewBag.AllSizes = sizesList.OrderBy(x => x).ToList();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("~/Areas/Admin/Views/Shared/_ProductList.cshtml", productVariants);
            }

            return View(productVariants);
        }

        [HttpGet]
        public async Task<IActionResult> EditVariant(int id)
        {
            if (id == 0) return NotFound();

            // Lấy variant kèm thông tin Product cha để hiển thị tên
            var variant = await db.ProductVariants
                                  .Include(v => v.Product)
                                  .ThenInclude(p => p.Category)
                                  .FirstOrDefaultAsync(v => v.ProductVariantId == id);

            if (variant == null) return NotFound();

            return View(variant);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditVariant(int id, ProductVariant variant, List<IFormFile> ImageFiles)
        {
            if (id != variant.ProductVariantId) return NotFound();

            // Include Product để lấy JSON Images cũ
            var variantInDb = await db.ProductVariants
                                      .Include(v => v.Product)
                                      .FirstOrDefaultAsync(v => v.ProductVariantId == id);

            if (variantInDb == null) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // --- 1. Cập nhật thông tin cơ bản ---
                    variantInDb.Size = variant.Size;
                    variantInDb.Price = variant.Price;
                    variantInDb.SalePrice = variant.SalePrice;
                    variantInDb.Stock = variant.Stock;
                    variantInDb.IsHidden = variant.IsHidden;
                    variantInDb.UpdatedAt = DateTime.Now;

                    // Lấy Slug cũ và mới
                    string oldColorSlug = ToUrlSlug(variantInDb.Color ?? "");
                    variantInDb.Color = variant.Color; // Cập nhật màu mới
                    string newColorSlug = ToUrlSlug(variant.Color ?? "");

                    // Lấy ID variant để làm key nhận diện file ảnh (QUAN TRỌNG)
                    string varIndex = variantInDb.ProductVariantId.ToString();

                    // --- 2. XỬ LÝ ẢNH ---
                    // Chỉ chạy logic ảnh nếu có upload ảnh mới HOẶC màu sắc thay đổi (cần đổi tên/xóa ảnh cũ)
                    bool isColorChanged = oldColorSlug != newColorSlug;
                    bool hasNewImages = ImageFiles != null && ImageFiles.Count > 0;

                    if (hasNewImages || isColorChanged)
                    {
                        // A. Lấy list ảnh hiện tại
                        List<string> currentImages = new List<string>();
                        if (!string.IsNullOrEmpty(variantInDb.Product.Images))
                        {
                            try { currentImages = JsonConvert.DeserializeObject<List<string>>(variantInDb.Product.Images) ?? new List<string>(); }
                            catch { }
                        }

                        // B. Danh sách ảnh CẦN GIỮ LẠI (Lọc ảnh cũ của variant này ra)
                        // LOGIC SỬA ĐỔI: Chỉ loại bỏ ảnh nếu tên file chứa ID của Variant này VÀ Slug màu cũ
                        var imagesToKeep = currentImages.Where(img =>
                        {
                            string fileName = Path.GetFileNameWithoutExtension(img);

                            // Pattern nhận diện ảnh của Variant này: Phải chứa "_ID_" VÀ "_oldColor"
                            // Ví dụ: ten-sp_101_red_1.png
                            string identifyPart = $"_{varIndex}_{oldColorSlug}";

                            // Nếu file chứa định danh cũ -> Bỏ qua (Return false để loại khỏi list)
                            if (fileName.Contains(identifyPart)) return false;

                            // Giữ lại các ảnh khác (ảnh của variant khác hoặc ảnh chung)
                            return true;
                        }).ToList();

                        // C. Xử lý Upload ảnh mới (Nếu có)
                        if (hasNewImages)
                        {
                            // Tạo đường dẫn (Logic cũ của bạn ok)
                            string genderFolder = GetGenderFolder(variantInDb.Product.TargetGroup ?? 0);
                            string categoryFolder = "edit-upload"; // Nên dynamic theo Cate thật nếu được
                            string productFolder = ToUrlSlug(variantInDb.Product.Name);
                            string relativePath = Path.Combine("images", "products", genderFolder, categoryFolder, productFolder);
                            string absolutePath = Path.Combine(_webHostEnvironment.WebRootPath, relativePath);

                            if (!Directory.Exists(absolutePath)) Directory.CreateDirectory(absolutePath);

                            // Đếm tiếp số thứ tự để đặt tên, tránh trùng lặp
                            int imgSubIndex = imagesToKeep.Count + 1;

                            foreach (var file in ImageFiles)
                            {
                                if (file.Length > 0)
                                {
                                    string ext = Path.GetExtension(file.FileName);

                                    // Đặt tên file chuẩn: TenSP_VarID_MauMoi_TimeStamp.ext
                                    // Dùng DateTime.Ticks để đảm bảo không bao giờ trùng tên kể cả khi F5
                                    string uniquePart = DateTime.Now.Ticks.ToString();
                                    string newFileName = $"{ToUrlSlug(variantInDb.Product.Name)}_{varIndex}_{newColorSlug}_{uniquePart}{ext}";

                                    string fullPath = Path.Combine(absolutePath, newFileName);

                                    using (var stream = new FileStream(fullPath, FileMode.Create))
                                    {
                                        await file.CopyToAsync(stream);
                                    }

                                    string dbPath = "/" + relativePath.Replace("\\", "/") + "/" + newFileName;
                                    imagesToKeep.Add(dbPath);
                                }
                            }
                        }

                        // D. Cập nhật JSON
                        variantInDb.Product.Images = JsonConvert.SerializeObject(imagesToKeep);
                        db.Entry(variantInDb.Product).State = EntityState.Modified;
                    }

                    db.Update(variantInDb);
                    await db.SaveChangesAsync();

                    return RedirectToAction("ListProductVariant", new { ProductId = variantInDb.ProductId });
                }
                catch (DbUpdateConcurrencyException)
                {
                    throw;
                }
            }
            variant.Product = variantInDb.Product;

            return View(variant);
        }


        [HttpGet]
        public async Task<IActionResult> ListCategory()
        {
            // Lấy danh sách chưa bị xóa mềm (DeletedAt == null)
            // Sắp xếp theo SortOrder (thứ tự ưu tiên)
            var categories = await db.Categories
                                     .Where(c => c.DeletedAt == null)
                                     .OrderBy(c => c.SortOrder)
                                     .ToListAsync();
            return View(categories);
        }

        [HttpGet]
        public IActionResult CreateCategory()
        {
            // Khởi tạo giá trị mặc định cho đẹp (Thứ tự = 1, Hiện = true)
            return View(new Category { SortOrder = 1, IsHidden = false });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(Category category)
        {
            // QUAN TRỌNG: Bỏ qua kiểm tra lỗi Slug vì ta sẽ tự tạo nó
            ModelState.Remove("Slug");

            if (ModelState.IsValid)
            {
                // Tự động tạo Slug từ Name
                category.Slug = ToUrlSlugCatagory(category.Name);

                category.CreatedAt = DateTime.Now;
                category.UpdatedAt = null;
                category.DeletedAt = null;

                // IsHidden đã được bind từ Form (checkbox), không cần set cứng = false
                // trừ khi bạn muốn ép buộc lúc tạo luôn hiện.

                db.Add(category);
                await db.SaveChangesAsync();
                return RedirectToAction(nameof(ListCategory));
            }
            return View(category);
        }

        [HttpGet]
        public async Task<IActionResult> EditCategory(int id)
        {
            if (id == 0) return NotFound();

            var category = await db.Categories.FindAsync(id);
            if (category == null || category.DeletedAt != null) return NotFound();

            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(int id, Category category)
        {
            if (id != category.CategoryId) return NotFound();

            // --- BƯỚC QUAN TRỌNG: Bỏ qua kiểm tra lỗi cho trường Slug ---
            // Vì Slug sẽ được tự động tạo lại bên dưới, không cần user nhập
            ModelState.Remove("Slug");

            if (ModelState.IsValid)
            {
                try
                {
                    var categoryInDb = await db.Categories.FindAsync(id);
                    if (categoryInDb == null) return NotFound();

                    categoryInDb.Name = category.Name;
                    categoryInDb.Description = category.Description;
                    categoryInDb.SortOrder = category.SortOrder;
                    categoryInDb.IsHidden = category.IsHidden;
                    categoryInDb.UpdatedAt = DateTime.Now;

                    // Cập nhật Slug mới
                    categoryInDb.Slug = ToUrlSlugCatagory(category.Name);

                    db.Update(categoryInDb);
                    await db.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryExists(category.CategoryId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(ListCategory));
            }

            // MẸO DEBUG: Nếu vẫn không lưu được, hãy đặt breakpoint ở đây 
            // hoặc in lỗi ra để xem nó đang bắt bẻ trường nào nữa
            // var errors = ModelState.Values.SelectMany(v => v.Errors); 

            return View(category);
        }

        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await db.Categories.FindAsync(id);
            if (category != null)
            {
                category.DeletedAt = DateTime.Now;
                category.IsHidden = !category.IsHidden;
                db.Update(category);
                await db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ListCategory));
        }

        private bool CategoryExists(int id)
        {
            return db.Categories.Any(e => e.CategoryId == id);
        }

        public static string ToUrlSlugCatagory(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";
            value = value.ToLowerInvariant();
            value = Regex.Replace(value, @"\s", "-", RegexOptions.Compiled);
            value = Regex.Replace(value, @"[^\w\s\p{Pd}]", "", RegexOptions.Compiled);
            value = value.Trim('-', '_');
            value = Regex.Replace(value, @"([-_]){2,}", "$1", RegexOptions.Compiled);
            return value;
        }


        public async Task<IActionResult> Sales()
        {
            // ----- INVENTORY -----
            var inventoryRaw = await db.ProductVariants
                .GroupBy(pv => pv.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    TotalStock = g.Sum(x => x.Stock)
                })
                .ToListAsync();

            var inventoryDict = inventoryRaw.ToDictionary(i => i.ProductId, i => i.TotalStock);

            var productsInventory = await db.Products
                .Where(p => inventoryDict.Keys.Contains(p.ProductId))
                .ToListAsync();

            var inventory = productsInventory
                .Select(p => new
                {
                    Product = p,
                    TotalStock = inventoryDict[p.ProductId]
                })
                .ToList();


            // ----- BEST SELLERS -----
            var bestRaw = await db.OrderItems
                .Where(oi => oi.Order.OrderStatus == OrderStatus.Completed)
                .GroupBy(oi => oi.ProductVariant.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    TotalSold = g.Sum(x => x.Quantity)
                })
                .OrderByDescending(x => x.TotalSold)
                .Take(10)
                .ToListAsync();

            var bestDict = bestRaw.ToDictionary(b => b.ProductId, b => b.TotalSold);

            var productsBest = await db.Products
                .Where(p => bestDict.Keys.Contains(p.ProductId))
                .ToListAsync(); // lấy product từ SQL trước

            var bestSellers = productsBest
                .Select(p => new
                {
                    Product = p,
                    TotalSold = bestDict[p.ProductId]
                })
                .OrderByDescending(x => x.TotalSold) // Sort tại RAM, không phải SQL
                .ToList();


            ViewBag.Inventory = inventory;
            ViewBag.BestSellers = bestSellers;

            return View();
        }

    }
}