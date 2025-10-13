using Clothing_shopping.Hubs;
using Clothing_shopping.models;
using Clothing_shopping.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Clothing_shopping.Controllers
{
    public class LoginController : Controller
    {
        private readonly ClothingContext db;
        private readonly IHubContext<AppHub> hubContext;

        public LoginController(ClothingContext _context, IHubContext<AppHub> _hubContext)
        {
            db = _context;
            hubContext = _hubContext;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email);

            if (user.IsLocked)
            {
                ViewBag.Error = "Tài khoản của bạn đã bị khoá. Vui lòng liên hệ quản trị viên để biết thêm chi tiết.";
                return View();
            }

            if (user != null && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                HttpContext.Session.SetString("UserId", user.UserId.ToString());
                HttpContext.Session.SetString("Email", user.Email.ToString());
                HttpContext.Session.SetString("FullName", user.FullName.ToString());
                await HttpContext.Session.CommitAsync();
                Console.WriteLine(">>> Đăng nhập thành công, SessionId: " + HttpContext.Session.Id);
                Console.WriteLine(">>> UserId: " + HttpContext.Session.GetString("UserId"));
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(string first_name, string second_name, string day, string month, string year, User u)
        {
            if (await db.Users.AnyAsync(x => x.Email == u.Email))
            {
                ViewBag.Error = "Email already exists";
                return View();
            }
            u.FullName = $"{first_name} {second_name}";
            u.Birthday = new DateOnly(int.Parse(year), int.Parse(month), int.Parse(day));
            u.PasswordHash = BCrypt.Net.BCrypt.HashPassword(u.PasswordHash);
            u.Role = "Customer";
            u.IsLocked = true;
            u.CreatedAt = DateTime.Now;
            u.IsDeleted = false;
            db.Users.Add(u);
            db.SaveChanges();
            return View("Login");
        }

        public async Task<IActionResult> LogOut()
        {
            var userid = HttpContext.Session.GetString("UserId");
            //var user = await db.Users.FindAsync(HttpContext.Session.GetString("UserId"));
            HttpContext.Session.Clear();
            //await hubContext.Clients.All.SendAsync("UserLoggedOut", userid);
            return RedirectToAction("Login", "Login");
        }

        [HttpGet]
        public IActionResult Profile()
        {
            return View();
        }

        //[HttpPost]
        //public async Task<IActionResult> Profile(string name)
        //{
        //    return View();
        //}
    }
}
