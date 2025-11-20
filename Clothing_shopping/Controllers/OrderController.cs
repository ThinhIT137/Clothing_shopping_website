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

        public IActionResult Index()
        {
            return View();
        }
    }
}
