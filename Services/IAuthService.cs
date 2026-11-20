namespace MedStockAlert.Services
{
    public interface IAuthService
    {
        /// <summary>
        /// ตรวจสอบชื่อผู้ใช้และรหัสผ่าน
        /// คืนค่า userId และ role ถ้าสำเร็จ, null ถ้าไม่สำเร็จ
        /// </summary>
        Task<(int UserId, string Username, string Role)?> ValidateCredentialsAsync(string username, string password);

        /// <summary>
        /// เปลี่ยนรหัสผ่าน (ใช้ในอนาคต)
        /// </summary>
        Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
    }
}
