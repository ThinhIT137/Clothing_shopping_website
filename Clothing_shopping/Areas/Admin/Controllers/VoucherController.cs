using Clothing_shopping.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Clothing_shopping.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class VoucherController : Controller
    {
        private readonly ClothingContext _context;

        public VoucherController(ClothingContext context)
        {
            _context = context;
        }

        // GET: Danh sách Voucher
        public async Task<IActionResult> Vouchers(
            DateTime? dateStart,
            DateTime? dateEnd,
            string? name,
            decimal? DiscountValue
        )
        {
            // 1. Tạo query cơ bản
            var query = _context.Vouchers.Include(v => v.Voucherusers).AsQueryable();

            // 2. Lọc theo Tên (Mã)
            if (!string.IsNullOrEmpty(name))
            {
                query = query.Where(v => v.Name.Contains(name));
            }

            // 3. Lọc theo Giá trị giảm (Nếu nhập 20 thì tìm những cái 20% hoặc 20k)
            if (DiscountValue.HasValue)
            {
                query = query.Where(v => v.DiscountValue == DiscountValue);
            }

            // 4. Lọc theo Khoảng thời gian (Tìm những voucher BẮT ĐẦU trong khoảng này)
            if (dateStart.HasValue)
            {
                query = query.Where(v => v.StartDate >= dateStart);
            }
            if (dateEnd.HasValue)
            {
                query = query.Where(v => v.StartDate <= dateEnd);
            }

            // 5. Sắp xếp và lấy dữ liệu
            var list = await query.OrderByDescending(v => v.CreatedAt).ToListAsync();

            // 6. Trả về Partial View nếu là AJAX
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                // Lưu ý: Đảm bảo đường dẫn file Partial đúng
                return PartialView("~/Areas/Admin/Views/Shared/_VouchersList.cshtml", list);
            }

            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Voucher model)
        {
            // 1. Kiểm tra trùng tên
            if (await _context.Vouchers.AnyAsync(v => v.Name == model.Name))
            {
                TempData["Error"] = "Mã voucher này đã tồn tại!";
                return RedirectToAction(nameof(Vouchers));
            }

            if (ModelState.IsValid)
            {
                // 2. Logic kiểm tra giá trị giảm giá
                if (model.VoucherType == "Percent")
                {
                    if (model.DiscountValue <= 0 || model.DiscountValue > 100)
                    {
                        TempData["Error"] = "Giá trị phần trăm phải từ 1 đến 100!";
                        return RedirectToAction(nameof(Vouchers));
                    }
                }
                else if (model.VoucherType == "Fixed")
                {
                    if (model.DiscountValue <= 0)
                    {
                        TempData["Error"] = "Số tiền giảm giá phải lớn hơn 0!";
                        return RedirectToAction(nameof(Vouchers));
                    }
                }

                // 3. Gán các giá trị mặc định
                model.CreatedAt = DateTime.Now;
                model.IsActive = true;
                model.UpdatedAt = DateTime.Now;

                // 4. Kiểm tra ngày tháng
                if (model.EndDate.HasValue && model.EndDate < model.StartDate)
                {
                    TempData["Error"] = "Ngày kết thúc phải lớn hơn ngày bắt đầu!";
                    return RedirectToAction(nameof(Vouchers));
                }

                _context.Add(model);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Tạo voucher thành công!";
            }
            return RedirectToAction(nameof(Vouchers));
        }

        // GET: Xem chi tiết Voucher và danh sách người dùng
        public async Task<IActionResult> Details(int id)
        {
            var voucher = await _context.Vouchers
                .Include(v => v.Voucherusers)
                .ThenInclude(vu => vu.User) // Load thông tin User
                .FirstOrDefaultAsync(v => v.VoucherId == id);

            if (voucher == null) return NotFound();

            return View(voucher);
        }

        // POST: Cập nhật số lượng cho từng người dùng (AJAX gọi vào đây)
        [HttpPost]
        public async Task<IActionResult> UpdateUserVoucher(int voucherId, Guid userId, int newAmount)
        {
            var relation = await _context.Voucherusers
                .FirstOrDefaultAsync(vu => vu.VoucherId == voucherId && vu.UserId == userId);

            if (relation != null)
            {
                if (newAmount <= 0)
                {
                    // Nếu chỉnh về 0 -> Xóa luôn quyền dùng voucher này
                    _context.Voucherusers.Remove(relation);
                }
                else
                {
                    // Cập nhật số lượng
                    relation.UsedCount = newAmount; // Hoặc AssignedCount tùy theo bro đang dùng cột nào
                    _context.Voucherusers.Update(relation);
                }
                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Cập nhật thành công!" });
            }

            return BadRequest("Không tìm thấy dữ liệu!");
        }

        // GET: Mở form sửa
        public async Task<IActionResult> Edit(int id)
        {
            var voucher = await _context.Vouchers.FindAsync(id);
            if (voucher == null) return NotFound();
            return View(voucher);
        }

        // POST: Lưu dữ liệu sửa
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Voucher model)
        {
            if (id != model.VoucherId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Logic kiểm tra giá trị giảm giá (giống lúc tạo)
                    if (model.VoucherType == "Percent" && (model.DiscountValue <= 0 || model.DiscountValue > 100))
                    {
                        ModelState.AddModelError("DiscountValue", "Phần trăm phải từ 1-100");
                        return View(model);
                    }

                    // Lấy voucher cũ từ DB ra để cập nhật (giữ nguyên ngày tạo)
                    var voucherInDb = await _context.Vouchers.FindAsync(id);
                    if (voucherInDb != null)
                    {
                        voucherInDb.Name = model.Name;
                        voucherInDb.Description = model.Description;
                        voucherInDb.VoucherType = model.VoucherType;
                        voucherInDb.DiscountValue = model.DiscountValue; // Cập nhật giá trị
                        voucherInDb.StartDate = model.StartDate;
                        voucherInDb.EndDate = model.EndDate;
                        voucherInDb.IsActive = model.IsActive;
                        voucherInDb.UpdatedAt = DateTime.Now; // Cập nhật thời gian sửa

                        _context.Update(voucherInDb);
                        await _context.SaveChangesAsync();
                        TempData["Success"] = "Cập nhật voucher thành công!";
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Vouchers.Any(e => e.VoucherId == id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Vouchers));
            }
            return View(model);
        }

        // GET: Xóa Voucher
        public async Task<IActionResult> Delete(int id)
        {
            var voucher = await _context.Vouchers.FindAsync(id);

            if (voucher != null)
            {
                // 2. Tìm TẤT CẢ những người đang sở hữu voucher này trong bảng VoucherUsers
                // Dùng .Where() chứ KHÔNG dùng .Find()
                var relatedRecords = _context.Voucherusers.Where(vu => vu.VoucherId == id);

                // 3. Xóa danh sách người sở hữu trước (Xóa con trước)
                _context.Voucherusers.RemoveRange(relatedRecords);

                // 4. Sau đó xóa Voucher chính (Xóa cha sau)
                _context.Vouchers.Remove(voucher);

                // 5. Lưu thay đổi
                await _context.SaveChangesAsync();

                TempData["Success"] = "Đã xóa voucher và dữ liệu liên quan!";
            }
            else
            {
                TempData["Error"] = "Không tìm thấy voucher để xóa!";
            }

            return RedirectToAction(nameof(Vouchers));
        }

        // GET: Hiển thị trang phát voucher
        public async Task<IActionResult> FlastSale()
        {
            // 1. Lấy danh sách Voucher đang hoạt động để Admin chọn
            ViewBag.Vouchers = await _context.Vouchers
                .Where(v => v.IsActive && (v.EndDate == null || v.EndDate > DateTime.Now))
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();

            // 2. Lấy danh sách Khách hàng (Chỉ lấy Role Customer)
            var customers = await _context.Users
                .Where(u => u.Role == "Customer" && u.IsDeleted == false)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            return View(customers);
        }

        // POST: Thực hiện phát voucher
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Distribute(int VoucherId, List<Guid> selectedUsers, int amountPerUser = 1)
        {
            // 1. Kiểm tra xem có chọn ai chưa
            if (selectedUsers == null || !selectedUsers.Any())
            {
                TempData["Error"] = "Chưa chọn khách hàng nào!";
                return RedirectToAction(nameof(FlastSale));
            }

            // Nếu quên nhập hoặc nhập số âm thì mặc định là 1
            if (amountPerUser <= 0) amountPerUser = 1;

            // 2. Lấy Voucher (BỎ ĐOẠN CHECK SỐ LƯỢNG <= 0)
            var voucher = await _context.Vouchers.FindAsync(VoucherId);
            if (voucher == null)
            {
                TempData["Error"] = "Voucher không tồn tại!";
                return RedirectToAction(nameof(FlastSale));
            }

            int successCount = 0; // Đếm số người nhận được
            int totalGiven = 0;   // Đếm tổng số voucher đã phát đi

            // 3. Chạy vòng lặp qua từng người được chọn
            foreach (var userId in selectedUsers)
            {
                // Lấy thẳng số lượng Admin nhập, không cần check kho
                int actualGive = amountPerUser;

                // Kiểm tra xem người này đã có voucher này chưa
                var userVoucher = await _context.Voucherusers
                    .FirstOrDefaultAsync(vu => vu.VoucherId == VoucherId && vu.UserId == userId);

                if (userVoucher != null)
                {
                    // CÓ RỒI -> Cộng thêm lượt dùng (UsedCount)
                    userVoucher.UsedCount += actualGive;
                    _context.Voucherusers.Update(userVoucher);
                }
                else
                {
                    // CHƯA CÓ -> Tạo mới
                    var newRelation = new Voucheruser
                    {
                        VoucherId = VoucherId,
                        UserId = userId,
                        UsedCount = actualGive, // Gán số lượt được dùng
                        LastUsedAt = null
                    };
                    _context.Voucherusers.Add(newRelation);
                }

                // BỎ ĐOẠN TRỪ KHO TỔNG (voucher.Quantity -= ...)

                totalGiven += actualGive;
                successCount++;
            }

            // 4. Lưu vào Database
            await _context.SaveChangesAsync();

            string msg = $"Đã phát xong {totalGiven} lượt dùng cho {successCount} khách hàng.";
            TempData["Success"] = msg;

            return RedirectToAction(nameof(FlastSale));
        }
    }
}