using Clothing_shopping.Hubs;
using Clothing_shopping.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace Clothing_shopping.Controllers
{
    public class UserController : Controller
    {
        private readonly ClothingContext db;

        public UserController(ClothingContext _context)
        {
            db = _context;
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

                var query = from nt in db.Notifications
                            join n in db.News on nt.NewsId equals n.NewsId
                            join u in db.Users on nt.ReceiverId equals u.UserId
                            where nt.ReceiverId == user.UserId && nt.IsRead == false
                            orderby nt.CreatedAt descending
                            select new
                            {
                                Title = n.Title.Replace("{OrderId}", nt.OrderId.ToString()),
                                Content = n.Content.Replace("{OrderId}", nt.OrderId.ToString()),
                                CreatedAt = nt.CreatedAt
                            };
                var newsList = await query.ToListAsync();
                HttpContext.Session.SetString("Notification", JsonConvert.SerializeObject(newsList, new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                }));

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
            u.IsLocked = false;
            u.CreatedAt = DateTime.Now;
            u.IsDeleted = false;
            db.Users.Add(u);
            db.SaveChanges();
            return View("Login");
        }

        public async Task<IActionResult> LogOut()
        {
            Console.WriteLine(">>> Đăng xuất, SessionId: " + HttpContext.Session.Id);
            Console.WriteLine(">>> UserId: " + HttpContext.Session.GetString("UserId"));
            HttpContext.Session.Clear();
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
