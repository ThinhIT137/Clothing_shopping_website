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

        public async Task<IActionResult> Order()
        {
            return View("Cart");
        }

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

        [HttpPost]
        public async Task<IActionResult> add_cart_product(int stock)
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString))
            {
                return Unauthorized(new { succes = false, message = "Bạn chưa đăng nhập" });
            }
            Guid.TryParse(userIdString, out Guid userId);


            return View();
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
