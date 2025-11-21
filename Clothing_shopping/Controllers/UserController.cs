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
            var userIdstring = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdstring))
            {
                return View();
            }
            Guid.TryParse(userIdstring, out Guid userId);

            return RedirectToAction("Index", "Home");
        }

        /* --- ĐĂNG NHẬP --- */
        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email);

            if (user is null)
            {
                ViewBag.Error = "Sai email hoặc mật khẩu!";
                return View();
            }

            if (user.IsLocked)
            {
                ViewBag.Error = "Tài khoản của bạn đã bị khoá. Vui lòng liên hệ quản trị viên để biết thêm chi tiết.";
                return View();
            }

            if (user != null && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                HttpContext.Session.SetString("UserId", user.UserId.ToString()); // lưu ID
                HttpContext.Session.SetString("Email", user.Email.ToString()); // lưu Email
                HttpContext.Session.SetString("FullName", user.FullName.ToString()); // Lưu fullname
                var newsList = await db.Notifications.Where(nt => nt.ReceiverId == user.UserId)
                                                     .Where(nt => nt.IsRead == false)
                                                     .Include(nt => nt.News) 
                                                     .OrderByDescending(nt => nt.CreatedAt)
                                                     .Select(nt => new
                                                     {
                                                         Title = nt.News.Title.Replace("{OrderId}", nt.OrderId.ToString()),
                                                         Content = nt.News.Content.Replace("{OrderId}", nt.OrderId.ToString()),
                                                         CreatedAt = nt.CreatedAt
                                                     })
                                                     .ToListAsync();
                HttpContext.Session.SetString("Notification", JsonConvert.SerializeObject(newsList, new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                }));

                    await HttpContext.Session.CommitAsync();
                    Console.WriteLine(">>> Đăng nhập thành công, SessionId: " + HttpContext.Session.Id);
                    Console.WriteLine(">>> UserId: " + HttpContext.Session.GetString("UserId"));
                    return RedirectToAction("Index", "Home");
                }
            }
            return View();
        }

        /* --- ĐĂNG KÝ --- */
        [HttpGet]
        public IActionResult Register()
        {
            var userIdstring = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdstring))
            {
                return View();
            }
            Guid.TryParse(userIdstring, out Guid userId);

            return RedirectToAction("Index", "Home");
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
            await db.Users.AddAsync(u);
            await db.SaveChangesAsync();
            return View("Login");
        }

        public async Task<IActionResult> LogOut()
        {
            Console.WriteLine(">>> Đăng xuất, SessionId: " + HttpContext.Session.Id);
            Console.WriteLine(">>> UserId: " + HttpContext.Session.GetString("UserId"));
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "User");
        }

        /* --- THÔNG TIN NGƯỜI DÙNG --- */
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
