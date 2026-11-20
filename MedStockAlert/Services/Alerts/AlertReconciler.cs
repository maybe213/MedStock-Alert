using MedStockAlert.Models;

namespace MedStockAlert.Services.Alerts;

public sealed class ReconcileResult
{
    public List<Alert> Created { get; } = new();
    public List<Alert> Reopened { get; } = new();
    public List<Alert> Resolved { get; } = new();
    /// <summary>รายการที่ควรส่ง LINE ในรอบนี้ (ผู้เรียกตั้ง LastNotifiedAt หลังส่งสำเร็จ)</summary>
    public List<Alert> ToNotify { get; } = new();
}

/// <summary>
/// เทียบผลจาก AlertEngine กับ Alert เดิมในฐานข้อมูล: สร้างใหม่ / เปิดซ้ำ / ปิดอัตโนมัติ / เลือกรายการที่ต้องเตือนซ้ำ
/// ฟังก์ชันนี้แก้ไข object Alert ที่ส่งเข้ามาโดยตรง ผู้เรียกมีหน้าที่บันทึกลงฐานข้อมูล
/// </summary>
public static class AlertReconciler
{
    public static ReconcileResult Reconcile(
        IEnumerable<Alert> existing,
        IEnumerable<AlertCandidate> candidates,
        AlertSettings settings,
        DateTime nowUtc)
    {
        var result = new ReconcileResult();
        var byKey = existing.ToDictionary(a => a.DedupeKey);
        var seen = new HashSet<string>();

        foreach (var c in candidates)
        {
            if (!seen.Add(c.DedupeKey)) continue;

            if (!byKey.TryGetValue(c.DedupeKey, out var alert))
            {
                alert = new Alert
                {
                    Type = c.Type,
                    Severity = c.Severity,
                    Status = AlertStatus.Open,
                    DedupeKey = c.DedupeKey,
                    Level = c.Level,
                    DrugId = c.DrugId,
                    BatchId = c.BatchId,
                    Message = c.Message,
                    FirstSeenAt = nowUtc
                };
                result.Created.Add(alert);
                result.ToNotify.Add(alert);
                continue;
            }

            alert.Message = c.Message;
            alert.Severity = c.Severity;

            switch (alert.Status)
            {
                case AlertStatus.Resolved:
                    // ปัญหากลับมาอีกครั้ง: เปิดใหม่และเตือนทันที
                    alert.Status = AlertStatus.Open;
                    alert.ResolvedAt = null;
                    alert.SnoozedUntil = null;
                    alert.FirstSeenAt = nowUtc;
                    alert.LastNotifiedAt = null;
                    result.Reopened.Add(alert);
                    result.ToNotify.Add(alert);
                    continue;

                case AlertStatus.Snoozed:
                    if (alert.SnoozedUntil.HasValue && alert.SnoozedUntil.Value > nowUtc) continue;
                    alert.Status = AlertStatus.Open;
                    alert.SnoozedUntil = null;
                    break;
            }

            if (IsDue(alert, settings, nowUtc)) result.ToNotify.Add(alert);
        }

        // เหตุการณ์ที่ไม่เกิดแล้ว (รับยาเพิ่ม / จ่ายออก / ทำลายแล้ว) -> ปิดอัตโนมัติ
        foreach (var alert in byKey.Values)
        {
            if (seen.Contains(alert.DedupeKey)) continue;
            if (alert.Status is AlertStatus.Open or AlertStatus.Snoozed)
            {
                alert.Status = AlertStatus.Resolved;
                alert.ResolvedAt = nowUtc;
                alert.SnoozedUntil = null;
                result.Resolved.Add(alert);
            }
        }

        return result;
    }

    /// <summary>ถึงรอบเตือนซ้ำหรือยัง (เทียบตามวัน ไม่เทียบชั่วโมง กันพลาดเพราะเวลารันเหลื่อมกันเล็กน้อย)</summary>
    private static bool IsDue(Alert alert, AlertSettings settings, DateTime nowUtc)
    {
        if (alert.LastNotifiedAt is null) return true;
        var interval = alert.Severity >= AlertSeverity.High
            ? settings.RepeatCriticalEveryDays
            : settings.RepeatNormalEveryDays;
        return (nowUtc.Date - alert.LastNotifiedAt.Value.Date).Days >= interval;
    }
}
