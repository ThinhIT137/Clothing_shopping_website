using Clothing_shopping.Hubs;
using Clothing_shopping.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Clothing_shopping.Controllers
{
    public class OrderController : Controller
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ClothingContext db = new ClothingContext();

        public OrderController(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        // gọi hub để real-time notification
        public async Task<IActionResult> CompleteOrder(int orderId)
        {
            var order = await db.Orders.FindAsync(orderId);
            order.OrderStatus = "Completed";
            await db.SaveChangesAsync();

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
