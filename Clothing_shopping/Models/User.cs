using Microsoft.AspNetCore.Identity;
using Microsoft.CodeAnalysis.Scripting;
using System.ComponentModel.DataAnnotations;

namespace Clothing_shopping.Models
{
    public class User
    {
        [Key]
        public Guid id_user { get; private set; } = Guid.NewGuid();
        [Required, MaxLength(100)]
        public string? fullname { get; set; }
        [Required, MaxLength(100)]
        [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Invalid email format")]
        public string? email { get; set; }
        [Required, MaxLength(100)]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Mật khẩu phải từ 8 đến 100 ký tự")]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*[a-z])(?=.*[0-9])(?=.*[^A-Za-z0-9]).{8,}$", ErrorMessage = "Mật khẩu phải có ít nhât 1 chữ hoa, 1 chữ thường, 1 số và 1 ký tự đặc biệt")]
        public string? password { get; private set; } = string.Empty;
        public string? phone { get; set; }
        public string? address { get; set; }
        public UserRole? Role { get; set; } = UserRole.Customer;
        public DateTime? created_at { get; set; } = DateTime.Now;
        public DateTime? updated_at { get; set; } = DateTime.Now;
        public User() { }
        public User(string fullname, string email, string password, string phone, string address, UserRole role)
        {
            this.fullname = fullname;
            this.email = email;
            this.password = password;
            this.phone = phone;
            this.address = address;
            this.Role = role;
        }
        public void SetPassword(string plainPassword)
        {
            password = BCrypt.Net.BCrypt.HashPassword(plainPassword);
        }
        public bool VerifyPassword(string plainPassword)
        {
            return BCrypt.Net.BCrypt.Verify(plainPassword, password);
        }
    }
    public enum UserRole
    {
        Admin,
        Customer
    }
}



