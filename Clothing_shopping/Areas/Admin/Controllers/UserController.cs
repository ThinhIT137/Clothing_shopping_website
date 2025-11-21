using Clothing_shopping.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace Clothing_shopping.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class UserController : Controller
    {
        private readonly ClothingContext db;

        public UserController(ClothingContext db)
        {
            this.db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Login()
        {
            var userIdstring = HttpContext.Session.GetString("UserIdAdmin");
            if (string.IsNullOrEmpty(userIdstring))
            {
                return View();
            }
            Guid.TryParse(userIdstring, out Guid userId);

            return RedirectToAction("Index", "Home", new { area = "Admin" });
        }

        /* --- ĐĂNG NHẬP --- */
        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email);
            if (user is null)
            {
                return View();
            }

            if (user.Role != "Admin")
            {
                return View();
            }

            if (user != null && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                HttpContext.Session.SetString("UserIdAdmin", user.UserId.ToString()); // lưu ID admin
                HttpContext.Session.SetString("Email", user.Email.ToString()); // lưu Email
                HttpContext.Session.SetString("FullName", user.FullName.ToString()); // Lưu fullname

                await HttpContext.Session.CommitAsync();
                Console.WriteLine(">>> Đăng nhập thành công, SessionId: " + HttpContext.Session.Id);
                Console.WriteLine(">>> UserId: " + HttpContext.Session.GetString("UserId"));
                return RedirectToAction("Index", "Home", new { area = "Admin" });
            }

            return View();
        }
        public async Task<IActionResult> LogOut()
        {
            Console.WriteLine(">>> Đăng xuất, SessionId: " + HttpContext.Session.Id);
            Console.WriteLine(">>> UserId: " + HttpContext.Session.GetString("UserIdAdmin"));
            HttpContext.Session.Remove("UserIdAdmin");
            return RedirectToAction("Login", "User", new { area = "Admin" });
        }

        public async Task<IActionResult> Profile()
        {
            var userIdString = HttpContext.Session.GetString("UserIdAdmin");
            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToAction("Login", "User", new { area = "Admin" });
            }

            Guid.TryParse(userIdString, out Guid userId);
            var user = await db.Users.FindAsync(userId);
            if (user == null)
            {
                return RedirectToAction("LogOut", "User", new { area = "Admin" });
            }

            return View(user);
        }

        [HttpGet]
        public async Task<IActionResult> Settings()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Settings(
            string? firstName,
            string? secondName,
            string? currentPassword,
            string? password,
            string? confirmPassword,
            string? address,
            string? phone,
            string? Gender,
            DateOnly? Birthday
        )
        {
            var userIdstring = HttpContext.Session.GetString("UserIdAdmin");
            if (string.IsNullOrEmpty(userIdstring)) // ID admin tồn tại
            {
                return RedirectToAction("Login", "User", new { area = "Admin" });
            }
            Guid.TryParse(userIdstring, out Guid userId);
            var user = await db.Users.FindAsync(userId);
            if (user == null) // Admin không tồn tại
            {
                return RedirectToAction("LogOut", "User", new { area = "Admin" });
            }

            if (!string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(secondName))
            {
                user.FullName = $"{firstName} {secondName}";
            }
            if (!string.IsNullOrWhiteSpace(password))
            {
                if (string.IsNullOrWhiteSpace(currentPassword))
                {
                    ViewBag.Error = "Bạn phải nhập mật khẩu cũ để đổi mật khẩu!";
                    return View(user);
                }

                if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
                {
                    ViewBag.Error = "Mật khẩu cũ không đúng!";
                    return View(user);
                }

                if (password != confirmPassword)
                {
                    ViewBag.Error = "Xác nhận mật khẩu mới không khớp!";
                    return View(user);
                }

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
            }
            if (!string.IsNullOrWhiteSpace(address))
            {
                user.Address = address;
            }
            if (!string.IsNullOrWhiteSpace(phone))
            {
                user.Phone = phone;
            }
            if (!string.IsNullOrWhiteSpace(Gender))
            {
                user.Gender = Gender;
            }
            if (Birthday.HasValue)
            {
                user.Birthday = Birthday;
            }
            await db.SaveChangesAsync();
            ViewBag.Success = "Cập nhật thông tin thành công!";
            return View(user);
        }
    }
}
