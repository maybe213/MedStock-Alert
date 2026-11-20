using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using MedStockAlert.Data;

namespace MedStockAlert.Services
{
    /// <summary>
    /// บริการยืนยันตัวตน
    /// ตอนนี้ใช้ mock user สำหรับพัฒนา
    /// เมื่อพร้อมให้เปลี่ยนไปอ่านจากตาราง Users ในฐานข้อมูล
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _db;

        // Mock users (ลบออกเมื่อมีตาราง Users จริง)
        private static readonly Dictionary<string, (string PasswordHash, int UserId, string Role)> MockUsers = new()
        {
            // username: pharmacist  password: MedStock@2026
            ["pharmacist"] = (HashPassword("MedStock@2026"), 1, "Pharmacist"),
            // username: admin       password: Admin@2026
            ["admin"] = (HashPassword("Admin@2026"), 2, "Admin")
        };

        public AuthService(AppDbContext db)
        {
            _db = db;
        }

        public Task<(int UserId, string Username, string Role)?> ValidateCredentialsAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return Task.FromResult<(int, string, string)?>(null);

            var key = username.Trim().ToLowerInvariant();

            if (MockUsers.TryGetValue(key, out var user))
            {
                var inputHash = HashPassword(password);
                if (CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(inputHash),
                    Encoding.UTF8.GetBytes(user.PasswordHash)))
                {
                    return Task.FromResult<(int, string, string)?>((user.UserId, username, user.Role));
                }
            }

            // TODO: เมื่อมีตาราง Users
            // var dbUser = await _db.Users.FirstOrDefaultAsync(u => u.Username == username && u.IsActive);
            // if (dbUser != null && VerifyPassword(password, dbUser.PasswordHash))
            //     return (dbUser.Id, dbUser.Username, dbUser.Role);

            return Task.FromResult<(int, string, string)?>(null);
        }

        public Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            // TODO: implement เมื่อมีตาราง Users
            return Task.FromResult(false);
        }

        private static string HashPassword(string password)
        {
            // ใช้ SHA256 ชั่วคราว — เปลี่ยนเป็น ASP.NET Identity PasswordHasher หรือ BCrypt ใน production
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password + "MedStockAlert.Salt.v1"));
            return Convert.ToHexString(bytes);
        }
    }
}

