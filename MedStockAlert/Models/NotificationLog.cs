using System.ComponentModel.DataAnnotations;

namespace MedStockAlert.Models;

/// <summary>ประวัติการส่งข้อความ (LINE) ไว้ตรวจสอบย้อนหลัง</summary>
public class NotificationLog
{
    public long Id { get; set; }
    public int? AlertId { get; set; }                      // null = ข้อความสรุปรายวัน
    public Alert? Alert { get; set; }

    [MaxLength(20)]
    public string Channel { get; set; } = "LINE";
    public string Payload { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? Error { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}
