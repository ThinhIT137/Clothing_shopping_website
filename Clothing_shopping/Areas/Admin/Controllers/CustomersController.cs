using Clothing_shopping.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Clothing_shopping.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class CustomersController : Controller
    {
        private readonly ClothingContext _context;
        public CustomersController(ClothingContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Customers(string? name, bool? isLocked, string? ChucVu)
        {
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrEmpty(ChucVu) && ChucVu != "All")
            {
                query = query.Where(u => u.Role == ChucVu);
            }

            if (!string.IsNullOrEmpty(name))
            {
                // Thêm điều kiện tìm gần đúng (Contains tương đương với LIKE %name% trong SQL)
                query = query.Where(u => u.FullName.Contains(name));
            }
            if (isLocked.HasValue)
            {
                query = query.Where(u => u.IsLocked == isLocked);
            }

            var users = await query.OrderByDescending(u => u.CreatedAt).ToListAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("~/Areas/Admin/Views/Shared/_Account.cshtml", users);
            }

            return View(users);
        }

        // ==========================================
        // 2. CHỨC NĂNG SỬA (EDIT)
        // ==========================================

        // GET: Hiển thị form sửa
        public async Task<IActionResult> Edit(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            return View(user);
        }

        // POST: Lưu dữ liệu sửa
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, User model)
        {
            if (id != model.UserId) return NotFound();

            // Lấy user gốc từ DB để tránh mất mật khẩu hoặc các trường không sửa
            var userInDb = await _context.Users.FindAsync(id);

            if (userInDb != null)
            {
                // Chỉ cập nhật các trường cho phép Admin sửa
                userInDb.FullName = model.FullName;
                userInDb.Phone = model.Phone;
                userInDb.Gender = model.Gender;
                // userInDb.Email = model.Email; // Thường Email không cho sửa

                _context.Update(userInDb);
                await _context.SaveChangesAsync();

                // Lưu xong quay về trang danh sách
                return RedirectToAction(nameof(Customers));
            }

            return View(model);
        }

        // ==========================================
        // 3. CHỨC NĂNG KHÓA / MỞ KHÓA (LOCK)
        // ==========================================
        public async Task<IActionResult> ToggleLock(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                // Đảo ngược trạng thái: Đang khóa -> Mở, Đang mở -> Khóa
                // Dùng (user.IsLocked ?? false) để xử lý nếu null thì coi là false
                user.IsLocked = !user.IsLocked;
                await _context.SaveChangesAsync();
            }
            // Xong thì reload lại trang danh sách
            return RedirectToAction(nameof(Customers));
        }

        public async Task<IActionResult> History(string type, string keyword)
        {
            // 1. NẾU LÀ AJAX REQUEST -> XỬ LÝ LỌC
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                // --- TRƯỜNG HỢP 1: TOP NGƯỜI MUA (Trả về List<User>) ---
                if (type == "TopBuyerDay" || type == "TopBuyerMonth")
                {
                    var dateLimit = (type == "TopBuyerDay") ? DateTime.Today : new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

                    // Lấy danh sách User, kèm theo đơn hàng trong khoảng thời gian đó để tính tổng
                    var topUsers = await _context.Users
                        .Where(u => u.Orders.Any(o => o.CreatedAt >= dateLimit)) // Chỉ lấy ai có mua
                        .Include(u => u.Orders) // Load đơn hàng để View tính tổng tiền
                        .ToListAsync();

                    // Sắp xếp bằng C# (Client evaluation) vì EF Core đôi khi khó sort theo Sum của List con
                    topUsers = topUsers
                        .OrderByDescending(u => u.Orders.Where(o => o.CreatedAt >= dateLimit).Sum(o => o.TotalAmount))
                        .Take(10)
                        .ToList();

                    // Truyền ngày lọc sang View để View biết đường tính tổng tiền hiển thị
                    ViewBag.FilterDate = dateLimit;

                    return PartialView("_TopUserList", topUsers);
                }

                // --- TRƯỜNG HỢP 2: TOP SẢN PHẨM (SỬA LẠI ĐỂ KHÔNG BỊ LỖI) ---
                else if (type == "TopProduct")
                {
                    // Bước 1: Chỉ lấy ID và Tổng số lượng (EF Core dịch được cái này sang SQL)
                    var stats = await _context.OrderItems
                        .GroupBy(oi => oi.ProductVariantId)
                        .Select(g => new
                        {
                            VariantId = g.Key,
                            TotalSold = g.Sum(x => x.Quantity),
                            // Lấy giá cao nhất hoặc trung bình để hiển thị tượng trưng
                            // Lưu ý: Không dùng g.First() ở đây sẽ gây lỗi
                            Price = g.Max(x => x.UnitPrice)
                        })
                        .OrderByDescending(x => x.TotalSold)
                        .Take(10)
                        .ToListAsync();

                    // Bước 2: Chuyển đổi dữ liệu sang List<OrderItem> để khớp với View
                    // (Làm ở bộ nhớ RAM nên không bị lỗi SQL)
                    var topProducts = new List<OrderItem>();

                    foreach (var item in stats)
                    {
                        topProducts.Add(new OrderItem
                        {
                            ProductVariantId = item.VariantId,
                            Quantity = item.TotalSold, // Mượn trường Quantity để lưu tổng số bán
                            UnitPrice = item.Price
                        });
                    }

                    return PartialView("_TopProductList", topProducts);
                }

                // --- TRƯỜNG HỢP 3: DANH SÁCH ĐƠN HÀNG (Mặc định) ---
                else
                {
                    var query = _context.Orders.Include(o => o.User).AsQueryable();

                    if (type == "HighValue") // Đơn nhiều tiền nhất
                    {
                        query = query.OrderByDescending(o => o.TotalAmount);
                    }
                    else if (type == "UserSearch" && !string.IsNullOrEmpty(keyword)) // Tìm theo tên
                    {
                        query = query.Where(o => o.User.FullName.Contains(keyword));
                        query = query.OrderByDescending(o => o.CreatedAt);
                    }
                    else // Mặc định: Mới nhất
                    {
                        query = query.OrderByDescending(o => o.CreatedAt);
                    }

                    var orders = await query.Take(50).ToListAsync();
                    return PartialView("_OrderList", orders);
                }
            }

            // 2. NẾU KHÔNG PHẢI AJAX -> TRẢ VỀ VIEW CHÍNH
            // Mặc định load danh sách đơn hàng
            var defaultOrders = await _context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.CreatedAt)
                .Take(20)
                .ToListAsync();

            return View(defaultOrders);
        }
    }
}
