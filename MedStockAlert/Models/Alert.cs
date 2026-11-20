using System.ComponentModel.DataAnnotations;

namespace MedStockAlert.Models;

/// <summary>รายการแจ้งเตือน 1 เรื่อง (กันส่งซ้ำด้วย DedupeKey)</summary>
public class Alert
{
    public int Id { get; set; }
    public AlertType Type { get; set; }
    public AlertSeverity Severity { get; set; }
    public AlertStatus Status { get; set; } = AlertStatus.Open;

    /// <summary>เช่น EXPIRY:30:{batchId}, LOW_STOCK:{drugId}, SLOW:{drugId}</summary>
    [MaxLength(100)]
    public string DedupeKey { get; set; } = string.Empty;
    public int? Level { get; set; }                        // ระดับวัน เช่น 180/90/30

    public int? DrugId { get; set; }
    public Drug? Drug { get; set; }
    public int? BatchId { get; set; }
    public Batch? Batch { get; set; }

    [MaxLength(500)]
    public string Message { get; set; } = string.Empty;
    public DateTime FirstSeenAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastNotifiedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? SnoozedUntil { get; set; }
}
