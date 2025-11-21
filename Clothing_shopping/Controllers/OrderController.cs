using Clothing_shopping.Hubs;
using Clothing_shopping.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Clothing_shopping.Controllers
{
    public class OrderController : Controller
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ClothingContext context;

        public OrderController(ClothingContext _context, IHubContext<NotificationHub> hubContext)
        {
            context = _context;
            _hubContext = hubContext;
        }

        [HttpGet]
        public async Task<IActionResult> Order()
        {


            return View("Cart");
        }

        [HttpPost]
        public async Task<IActionResult> Order(CartItem i)
        {
            return View("Cart");
        }

        /* --- GIỎ HÀNG --- */
        [HttpGet]
        public async Task<IActionResult> Cart()
        {
            var userIDString = HttpContext.Session.GetString("UserId");
            Guid userID;
            if (string.IsNullOrEmpty(userIDString) || !Guid.TryParse(userIDString, out userID))
            {
                return RedirectToAction("Login", "User");
            }
            var cartItem = await context.CartItems.Where(c => c.UserId == userID)
                                            .Include(c => c.ProductVariant).ThenInclude(c => c.Product)
                                            .OrderByDescending(c => c.AddedAt)
                                            .AsNoTracking()
                                            .ToListAsync();

            return View(cartItem);
        }

        /* --- CHỈNH SỬA SỐ LƯỢNG --- */
        [HttpPost]
        public async Task<IActionResult> edit_quatity_cartItem(int quatity, int CartItemId)
        {
            Console.WriteLine(quatity + " " + CartItemId);
            var itemID = await context.CartItems.FindAsync(CartItemId);
            if (itemID != null)
            {
                itemID.Quantity = quatity;
                await context.SaveChangesAsync();
            }
            return Json(new { success = true, message = "Chỉnh sửa số lượng thành công" });
        }

        /* --- THÊM SẢN PHẨM VÀO GIỎ HÀNG --- */
        [HttpPost]
        public async Task<IActionResult> add_cart_product(int ProductVariantId, int Quantity)
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString))
            {
                return Unauthorized(new { success = false, message = "Bạn chưa đăng nhập" });
            }
            Guid.TryParse(userIdString, out Guid userId);

            Console.WriteLine(ProductVariantId + " " + Quantity);

            var CartItem = new CartItem
            {
                UserId = userId,
                ProductVariantId = ProductVariantId,
                Quantity = Quantity
            };

            Console.WriteLine(">>> Thêm vào giỏ hàng: " + CartItem.UserId + " - " + CartItem.ProductVariantId + " - " + CartItem.Quantity);

            await context.CartItems.AddAsync(CartItem);
            await context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã thêm vào giỏ hàng thành công!" });
        }

        /* --- XÓA SẢN PHẨM GIỎ HÀNG --- */
        public async Task<IActionResult> delete_cart_product(int id)
        {
            var itemID = await context.CartItems.FindAsync(id);
            if (itemID != null)
            {
                context.CartItems.Remove(itemID);
                await context.SaveChangesAsync();
            }
            return RedirectToAction("Cart", "Order");
        }

        // gọi hub để real-time notification
        public async Task<IActionResult> CompleteOrder(int orderId)
        {
            var order = await context.Orders.FindAsync(orderId);
            order.OrderStatus = "Completed";
            await context.SaveChangesAsync();

            var message = $"Đơn hàng #{order.OrderId} của bạn đã hoàn tất!";
            await _hubContext.Clients.User(order.UserId.ToString())
                .SendAsync("ReceiveNotification", message);

            return Ok();
        }

        // 1. Sửa Action Checkout: Nhận thêm tham số productVariantId và quantity
        [HttpPost]
        public async Task<IActionResult> Checkout(List<int>? selectedIds, int? productVariantId, int? quantity)
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString)) return RedirectToAction("Login", "User");
            Guid userId = Guid.Parse(userIdString);
            var user = await context.Users.FindAsync(userId);

            var model = new CheckoutViewModel
            {
                ReceiverName = user?.FullName,
                Phone = user?.Phone,
                Email = user?.Email
            };

            // TRƯỜNG HỢP 1: Mua từ giỏ hàng (có selectedIds)
            if (selectedIds != null && selectedIds.Any())
            {
                model.SelectedCartItemIds = selectedIds;
                model.CartItemsDisplay = await context.CartItems
                    .Where(c => selectedIds.Contains(c.CartItemId) && c.UserId == userId)
                    .Include(c => c.ProductVariant).ThenInclude(p => p.Product)
                    .ToListAsync();
            }
            // TRƯỜNG HỢP 2: Mua ngay (có productVariantId)
            else if (productVariantId.HasValue && quantity.HasValue)
            {
                // Lưu thông tin để lát nữa submit form PlaceOrder dùng
                model.DirectProductVariantId = productVariantId;
                model.DirectQuantity = quantity;

                // Lấy thông tin sản phẩm từ DB để hiển thị lên View (Tạo CartItem ảo)
                var variant = await context.ProductVariants
                    .Include(v => v.Product)
                    .FirstOrDefaultAsync(v => v.ProductVariantId == productVariantId);

                if (variant != null)
                {
                    model.CartItemsDisplay = new List<CartItem>
            {
                new CartItem
                {
                    CartItemId = 0, // ID giả
                    ProductVariant = variant,
                    ProductVariantId = variant.ProductVariantId,
                    Quantity = quantity.Value,
                    UserId = userId
                }
            };
                }
            }
            else
            {
                TempData["Error"] = "Không có sản phẩm nào để thanh toán";
                return RedirectToAction("Cart");
            }

            // --- Lấy Voucher (Giữ nguyên logic cũ) ---
            model.AvailableVouchers = await context.Voucherusers
                .Include(vu => vu.Voucher)
                .Where(vu => vu.UserId == userId && vu.UsedCount > 0 && vu.Voucher.IsActive
                             && vu.Voucher.StartDate <= DateTime.Now
                             && (vu.Voucher.EndDate == null || vu.Voucher.EndDate > DateTime.Now))
                .Select(vu => vu.Voucher)
                .ToListAsync();

            return View(model);
        }

        // 2. Sửa Action PlaceOrder: Xử lý lưu đơn hàng
        [HttpPost]
        public async Task<IActionResult> PlaceOrder(CheckoutViewModel model)
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString)) return RedirectToAction("Login", "User");
            Guid userId = Guid.Parse(userIdString);

            if (ModelState.IsValid)
            {
                List<CartItem> itemsToOrder = new List<CartItem>();

                // LOGIC PHÂN LOẠI NGUỒN HÀNG
                if (model.DirectProductVariantId.HasValue)
                {
                    // --> Xử lý Mua Ngay: Lấy giá trực tiếp từ bảng ProductVariant
                    var variant = await context.ProductVariants
                        .FirstOrDefaultAsync(v => v.ProductVariantId == model.DirectProductVariantId);

                    if (variant != null)
                    {
                        itemsToOrder.Add(new CartItem
                        {
                            ProductVariantId = variant.ProductVariantId,
                            ProductVariant = variant, // Gán để tính giá bên dưới
                            Quantity = model.DirectQuantity ?? 1
                        });
                    }
                }
                else
                {
                    // --> Xử lý Mua từ Giỏ: Lấy từ bảng CartItems
                    itemsToOrder = await context.CartItems
                        .Where(c => model.SelectedCartItemIds.Contains(c.CartItemId) && c.UserId == userId)
                        .Include(c => c.ProductVariant)
                        .ToListAsync();
                }

                if (!itemsToOrder.Any()) return RedirectToAction("Cart");

                // --- TÍNH TOÁN GIÁ & VOUCHER (Giống hệt logic cũ) ---
                var groupedItems = itemsToOrder
                    .GroupBy(c => c.ProductVariantId)
                    .Select(g => new {
                        ProductVariantId = g.Key,
                        Quantity = g.Sum(c => c.Quantity),
                        Price = (g.First().ProductVariant.SalePrice > 0 ? g.First().ProductVariant.SalePrice : null) ?? g.First().ProductVariant.Price ?? 0
                    }).ToList();

                decimal subTotal = groupedItems.Sum(x => x.Quantity * x.Price);
                decimal discountAmount = 0;

                // Xử lý Voucher 
                if (model.SelectedVoucherId.HasValue)
                {
                    var voucherUser = await context.Voucherusers.Include(vu => vu.Voucher)
                       .FirstOrDefaultAsync(vu => vu.UserId == userId && vu.VoucherId == model.SelectedVoucherId);

                    if (voucherUser != null && voucherUser.UsedCount > 0
                        && voucherUser.Voucher.IsActive
                        && voucherUser.Voucher.StartDate <= DateTime.Now
                        && (voucherUser.Voucher.EndDate == null || voucherUser.Voucher.EndDate > DateTime.Now))
                    {
                        if (voucherUser.Voucher.VoucherType == "Percent") discountAmount = subTotal * (voucherUser.Voucher.DiscountValue / 100);
                        else discountAmount = voucherUser.Voucher.DiscountValue;

                        voucherUser.UsedCount--; // Trừ lượt dùng
                        context.Voucherusers.Update(voucherUser);
                    }
                }

                decimal finalTotal = subTotal - discountAmount;
                if (finalTotal < 0) finalTotal = 0;

                // 3. Lưu Order
                var order = new Order
                {
                    UserId = userId,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    TotalAmount = finalTotal,
                    OrderStatus = "Pending",
                    ShippingAddress = $"{model.ReceiverName} - {model.ShippingAddress}",
                    Phone = model.Phone,
                    Email = model.Email
                };
                context.Orders.Add(order);
                await context.SaveChangesAsync();

                // 4. Lưu OrderItem
                foreach (var item in groupedItems)
                {
                    context.OrderItems.Add(new OrderItem
                    {
                        OrderId = order.OrderId,
                        ProductVariantId = item.ProductVariantId,
                        Quantity = item.Quantity,
                        UnitPrice = item.Price,
                        TotalMoney = item.Price * item.Quantity
                    });
                }

                // 5. QUAN TRỌNG: Chỉ xóa giỏ hàng NẾU mua từ giỏ (Không có DirectID)
                if (!model.DirectProductVariantId.HasValue)
                {
                    context.CartItems.RemoveRange(itemsToOrder);
                }

                await context.SaveChangesAsync();

                // 6. Thông báo
                await _hubContext.Clients.User(userIdString).SendAsync("ReceiveNotification", $"Đơn hàng #{order.OrderId} đặt thành công!");
                TempData["Success"] = "Đặt hàng thành công!";
                return RedirectToAction("Profile", "User", new { tab = "orders" });
            }

            // Load lại data nếu lỗi Form
            if (model.DirectProductVariantId.HasValue)
            {
                // Load lại item mua ngay
                var v = await context.ProductVariants.Include(p => p.Product).FirstOrDefaultAsync(x => x.ProductVariantId == model.DirectProductVariantId);
                if (v != null) model.CartItemsDisplay = new List<CartItem> { new CartItem { ProductVariant = v, Quantity = model.DirectQuantity ?? 1 } };
            }
            else
            {
                // Load lại item giỏ hàng
                model.CartItemsDisplay = await context.CartItems.Where(c => model.SelectedCartItemIds.Contains(c.CartItemId)).Include(c => c.ProductVariant).ThenInclude(p => p.Product).ToListAsync();
            }

            // Load lại voucher
            model.AvailableVouchers = await context.Voucherusers.Include(vu => vu.Voucher).Where(vu => vu.UserId == userId && vu.UsedCount > 0 && vu.Voucher.IsActive).Select(vu => vu.Voucher).ToListAsync();

            return View("Checkout", model);
        }
    }
}
