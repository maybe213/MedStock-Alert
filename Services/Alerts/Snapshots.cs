using MedStockAlert.Models;

namespace MedStockAlert.Services.Alerts;

// ข้อมูลอ่านอย่างเดียวที่ชั้นข้อมูลดึงจากฐานข้อมูลมาให้ AlertEngine
// (แยกจาก EF Entity เพื่อให้กฎธุรกิจทดสอบได้โดยไม่ต้องใช้ฐานข้อมูล)

public sealed record BatchSnapshot(
    int BatchId,
    string LotNo,
    DateOnly ExpiryDate,
    DateOnly ReceivedDate,
    int QuantityOnHand,
    BatchStatus Status);

public sealed record PolicySnapshot(
    int MinQty = 0,
    int MaxQty = 0,
    bool UseAutoReorderPoint = true,
    int? ManualReorderPoint = null,
    int? LeadTimeDays = null,
    int? SlowMovingDays = null,
    string? ExpiryWarnDays = null);

public sealed record DrugStockSnapshot
{
    public int DrugId { get; init; }
    public string DrugCode { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Unit { get; init; } = string.Empty;
    public IReadOnlyList<BatchSnapshot> Batches { get; init; } = Array.Empty<BatchSnapshot>();
    public PolicySnapshot? Policy { get; init; }
    /// <summary>อัตราจ่ายเฉลี่ยต่อวัน (เช่น 30 วันย้อนหลัง) คำนวณโดยชั้นข้อมูล</summary>
    public decimal AvgDailyUsage { get; init; }
    public DateOnly? LastDispensedDate { get; init; }
}

public sealed record AlertCandidate(
    AlertType Type,
    AlertSeverity Severity,
    string DedupeKey,
    string Message,
    int? Level = null,
    int? DrugId = null,
    int? BatchId = null);
