using Clothing_shopping.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Clothing_shopping.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class OrderController : Controller
    {
        private readonly ClothingContext db;

        public OrderController(ClothingContext context)
        {
            db = context;
        }

        // --- 1. DANH SÁCH CHỜ XÁC NHẬN ---
        [HttpGet]
        public async Task<IActionResult> ListOrderPending()
        {
            // Sửa logic: So sánh chuỗi "Pending" (hoặc từ khóa bạn dùng trong DB)
            var orders = await db.Orders
                                 .Where(o => o.OrderStatus == OrderStatus.Pending)
                                 .OrderByDescending(o => o.CreatedAt)
                                 .ToListAsync();
            return View(orders);
        }

        // --- 2. XEM CHI TIẾT (Giữ nguyên) ---
        [HttpGet]
        public async Task<IActionResult> OrderDetails(int id)
        {
            var order = await db.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.ProductVariant) // Lấy thông tin biến thể (Màu, Size)
                .ThenInclude(pv => pv.Product)        // Lấy thông tin sản phẩm (Tên, Ảnh)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null) return NotFound();
            return View(order);
        }

        // --- 3. XÁC NHẬN ĐƠN HÀNG ---
        public async Task<IActionResult> ConfirmOrder(int id)
        {
            var order = await db.Orders.FindAsync(id);

            // Kiểm tra nếu đúng là đang "Pending" thì mới cho chuyển
            if (order != null && order.OrderStatus == OrderStatus.Pending)
            {
                // Chuyển sang trạng thái Đóng gói
                order.OrderStatus = OrderStatus.Packing;
                order.UpdatedAt = DateTime.Now;

                db.Update(order);
                await db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ListOrderPending));
        }

        // --- 4. HỦY ĐƠN HÀNG ---
        public async Task<IActionResult> CancelOrder(int id)
        {
            var order = await db.Orders.FindAsync(id);
            if (order != null)
            {
                // Chuyển sang trạng thái Hủy
                order.OrderStatus = OrderStatus.Cancelled;
                order.UpdatedAt = DateTime.Now;

                db.Update(order);
                await db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ListOrderPending));
        }

        // --- 5. DANH SÁCH ĐANG ĐÓNG GÓI (Packing) ---
        [HttpGet]
        public async Task<IActionResult> ListOrderPacking()
        {
            // Lấy các đơn có trạng thái "Packing"
            var orders = await db.Orders
                                 .Where(o => o.OrderStatus == "Packing")
                                 .OrderByDescending(o => o.UpdatedAt) // Sắp xếp theo ngày cập nhật mới nhất
                                 .ToListAsync();
            return View(orders);
        }

        // --- 6. BẮT ĐẦU GIAO HÀNG (Chuyển sang Shipping) ---
        public async Task<IActionResult> StartShipping(int id)
        {
            var order = await db.Orders.FindAsync(id);

            // Chỉ đơn đang đóng gói mới được đi giao
            if (order != null && order.OrderStatus == "Packing")
            {
                order.OrderStatus = "Shipping"; // Chuyển trạng thái
                order.UpdatedAt = DateTime.Now;

                db.Update(order);
                await db.SaveChangesAsync();
            }
            // Load lại trang Đóng gói để thấy đơn đó đã bay màu (sang trang Giao hàng)
            return RedirectToAction(nameof(ListOrderPacking));
        }

        // --- 7. DANH SÁCH ĐANG GIAO (Shipping) ---
        [HttpGet]
        public async Task<IActionResult> ListOrderShipping()
        {
            // Lấy các đơn có trạng thái "Shipping"
            var orders = await db.Orders
                                 .Where(o => o.OrderStatus == "Shipping")
                                 .OrderByDescending(o => o.UpdatedAt)
                                 .ToListAsync();
            return View(orders);
        }

        // --- 8. HOÀN THÀNH ĐƠN HÀNG (Chuyển sang Completed) ---
        public async Task<IActionResult> CompleteOrder(int id)
        {
            var order = await db.Orders.FindAsync(id);

            // Chỉ đơn đang đi giao mới được phép hoàn thành
            if (order != null && order.OrderStatus == "Shipping")
            {
                order.OrderStatus = "Completed"; // Chuyển trạng thái thành công
                order.UpdatedAt = DateTime.Now;

                // (Optional) Nếu có logic cộng điểm tích lũy cho User thì viết ở đây

                db.Update(order);
                await db.SaveChangesAsync();
            }

            // Load lại trang để thấy đơn đó biến mất (chuyển sang mục Hoàn thành)
            return RedirectToAction(nameof(ListOrderShipping));
        }

        // (Optional) Giao thất bại / Bom hàng -> Chuyển về Hủy hoặc Trả hàng
        public async Task<IActionResult> FailShipping(int id)
        {
            var order = await db.Orders.FindAsync(id);
            if (order != null && order.OrderStatus == "Shipping")
            {
                order.OrderStatus = "Cancelled"; // Hoặc tạo trạng thái riêng như "Returned"
                order.UpdatedAt = DateTime.Now;
                db.Update(order);
                await db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ListOrderShipping));
        }

        // --- 9. DANH SÁCH ĐÃ HOÀN THÀNH (Completed) ---
        [HttpGet]
        public async Task<IActionResult> ListOrderCompleted()
        {
            // Lấy các đơn có trạng thái "Completed"
            // Sắp xếp: Đơn nào mới hoàn thành thì hiện lên đầu
            var orders = await db.Orders
                                 .Where(o => o.OrderStatus == "Completed")
                                 .OrderByDescending(o => o.UpdatedAt)
                                 .ToListAsync();
            return View(orders);
        }

        // (Optional) Nếu lỡ bấm nhầm hoàn thành, muốn hoàn tác lại trạng thái Shipping?
        // Chỉ dùng cho Admin cấp cao, nhưng tôi viết mẫu ở đây nếu bạn cần.
        public async Task<IActionResult> RevertToShipping(int id)
        {
            var order = await db.Orders.FindAsync(id);
            if (order != null && order.OrderStatus == "Completed")
            {
                order.OrderStatus = "Shipping";
                order.UpdatedAt = DateTime.Now;
                db.Update(order);
                await db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ListOrderCompleted));
        }

        // Action lấy danh sách đơn hủy
        public async Task<IActionResult> Cancelled()
        {
            var cancelledOrders = await db.Orders
                .Include(o => o.User) // Join bảng User để biết ai hủy
                .Include(o => o.OrderItems) // (Tùy chọn) Load thêm chi tiết để hiện số lượng món
                .Where(o => o.OrderStatus == OrderStatus.Cancelled)
                .OrderByDescending(o => o.CreatedAt) // Đơn mới hủy hiện lên đầu
                .ToListAsync();

            return View(cancelledOrders);
        }
    }
}
