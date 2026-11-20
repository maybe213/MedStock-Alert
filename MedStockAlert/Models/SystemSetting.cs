using System.ComponentModel.DataAnnotations;

namespace MedStockAlert.Models;

/// <summary>ค่าตั้งค่ากลางของระบบ (Key-Value) ปรับได้จากหน้าเว็บ</summary>
public class SystemSetting
{
    [Key, MaxLength(60)]
    public string Key { get; set; } = string.Empty;
    [MaxLength(500)]
    public string Value { get; set; } = string.Empty;
    [MaxLength(200)]
    public string? Description { get; set; }
}
