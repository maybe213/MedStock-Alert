using MedStockAlert.Models;

namespace MedStockAlert.Services.Alerts;

/// <summary>
/// ประเมินกฎแจ้งเตือนทั้งหมดจากสต็อกปัจจุบัน (ฟังก์ชันล้วน ไม่แตะฐานข้อมูล ทดสอบง่าย)
/// กฎ: หมดอายุแล้ว · ใกล้หมดอายุ (หลายระดับ) · ต่ำกว่า Min · ถึงจุดสั่งซื้อ · เกิน Max · ยาค้างสต็อก
/// </summary>
public static class AlertEngine
{
    public static List<AlertCandidate> Evaluate(
        IEnumerable<DrugStockSnapshot> drugs, AlertSettings settings, DateOnly today)
    {
        var result = new List<AlertCandidate>();
        foreach (var drug in drugs)
        {
            EvaluateExpiry(drug, settings, today, result);
            EvaluateStockLevel(drug, settings, today, result);
            EvaluateSlowMoving(drug, settings, today, result);
        }
        return result;
    }

    // ---------- 1) หมดอายุ / ใกล้หมดอายุ (ระดับล็อต) ----------
    private static void EvaluateExpiry(
        DrugStockSnapshot drug, AlertSettings settings, DateOnly today, List<AlertCandidate> output)
    {
        var tiers = AlertSettings.ParseDays(drug.Policy?.ExpiryWarnDays, settings.ExpiryWarnDays);

        foreach (var batch in drug.Batches)
        {
            if (batch.QuantityOnHand <= 0 || batch.Status == BatchStatus.Depleted) continue;

            var days = batch.ExpiryDate.DayNumber - today.DayNumber;
            var qtyText = $"{batch.QuantityOnHand:N0} {drug.Unit}";

            if (days < 0)
            {
                output.Add(new AlertCandidate(
                    AlertType.Expired, AlertSeverity.Critical,
                    $"EXPIRED:{batch.BatchId}",
                    $"{drug.Name} Lot {batch.LotNo} หมดอายุ {ThaiDate.Format(batch.ExpiryDate)} คงเหลือ {qtyText}",
                    DrugId: drug.DrugId, BatchId: batch.BatchId));
                continue;
            }

            var tierIndex = -1;
            for (var i = 0; i < tiers.Count; i++)
            {
                if (days <= tiers[i]) { tierIndex = i; break; }
            }
            if (tierIndex < 0) continue;

            var tier = tiers[tierIndex];
            var severity = tierIndex == 0 ? AlertSeverity.High
                         : tierIndex == 1 ? AlertSeverity.Medium
                         : AlertSeverity.Low;

            output.Add(new AlertCandidate(
                AlertType.ExpiringSoon, severity,
                $"EXPIRY:{tier}:{batch.BatchId}",
                $"{drug.Name} Lot {batch.LotNo} หมดอายุ {ThaiDate.Format(batch.ExpiryDate)} (อีก {days} วัน) คงเหลือ {qtyText}",
                Level: tier, DrugId: drug.DrugId, BatchId: batch.BatchId));
        }
    }

    // ---------- 2) ระดับสต็อก: Min / Reorder Point / Max (ระดับยา) ----------
    // สต็อกที่ "ใช้ได้" = ล็อต Active ที่เหลืออายุเกิน BlockDispenseWithinDays วัน
    // (ล็อตที่งดจ่ายหรือกักกันไม่นับ เพราะจ่ายจริงไม่ได้)
    private static void EvaluateStockLevel(
        DrugStockSnapshot drug, AlertSettings settings, DateOnly today, List<AlertCandidate> output)
    {
        var policy = drug.Policy;
        if (policy is null) return;

        var cutoff = today.AddDays(settings.BlockDispenseWithinDays);
        var usable = drug.Batches
            .Where(b => b.QuantityOnHand > 0 && b.Status == BatchStatus.Active && b.ExpiryDate > cutoff)
            .Sum(b => b.QuantityOnHand);

        var min = policy.MinQty;
        var max = policy.MaxQty;
        var rop = ReorderPoint(drug, settings);

        string OrderHint(int target) =>
            target > usable ? $" แนะนำสั่ง {target - usable:N0} {drug.Unit}" : string.Empty;

        if (min > 0 && usable <= min)
        {
            output.Add(new AlertCandidate(
                AlertType.BelowMin, AlertSeverity.High,
                $"BELOWMIN:{drug.DrugId}",
                $"{drug.Name} คงเหลือใช้ได้ {usable:N0} {drug.Unit} (Min {min:N0}){OrderHint(max)}",
                DrugId: drug.DrugId));
        }
        else if (rop > 0 && usable <= rop)
        {
            output.Add(new AlertCandidate(
                AlertType.ReorderPoint, AlertSeverity.Medium,
                $"REORDER:{drug.DrugId}",
                $"{drug.Name} คงเหลือใช้ได้ {usable:N0} {drug.Unit} (จุดสั่งซื้อ {rop:N0}){OrderHint(max)}",
                DrugId: drug.DrugId));
        }

        if (max > 0 && usable > max)
        {
            output.Add(new AlertCandidate(
                AlertType.OverMax, AlertSeverity.Info,
                $"OVERMAX:{drug.DrugId}",
                $"{drug.Name} คงเหลือใช้ได้ {usable:N0} {drug.Unit} เกิน Max {max:N0}",
                DrugId: drug.DrugId));
        }
    }

    /// <summary>จุดสั่งซื้อ: ใช้ค่ากำหนดเองถ้าตั้งไว้ ไม่เช่นนั้น = ceil(อัตราจ่าย/วัน × LeadTime) + Min</summary>
    public static int ReorderPoint(DrugStockSnapshot drug, AlertSettings settings)
    {
        var policy = drug.Policy;
        if (policy is null) return 0;
        if (!policy.UseAutoReorderPoint && policy.ManualReorderPoint.HasValue)
            return policy.ManualReorderPoint.Value;

        var lead = policy.LeadTimeDays ?? settings.DefaultLeadTimeDays;
        return (int)Math.Ceiling(drug.AvgDailyUsage * lead) + policy.MinQty;
    }

    // ---------- 3) ยาค้างสต็อก (ระดับยา) ----------
    // นับจาก "กิจกรรมล่าสุด" = วันจ่ายล่าสุด หรือวันรับเข้าล่าสุด แล้วแต่อย่างไหนใหม่กว่า
    // (เพิ่งรับยาเข้ามา ไม่ควรถูกนับว่าค้างแม้ยังไม่เคยจ่าย)
    private static void EvaluateSlowMoving(
        DrugStockSnapshot drug, AlertSettings settings, DateOnly today, List<AlertCandidate> output)
    {
        var threshold = drug.Policy?.SlowMovingDays ?? settings.DefaultSlowMovingDays;
        if (threshold <= 0) return;

        var inStock = drug.Batches.Where(b => b.QuantityOnHand > 0 && b.ExpiryDate >= today).ToList();
        if (inStock.Count == 0) return;

        var lastReceived = inStock.Max(b => b.ReceivedDate);
        var lastActivity = drug.LastDispensedDate.HasValue && drug.LastDispensedDate.Value > lastReceived
            ? drug.LastDispensedDate.Value
            : lastReceived;

        var idleDays = today.DayNumber - lastActivity.DayNumber;
        if (idleDays < threshold) return;

        var total = inStock.Sum(b => b.QuantityOnHand);
        var basis = drug.LastDispensedDate is null ? "ยังไม่เคยจ่าย นับจากวันรับเข้า" : "ไม่มีการจ่าย";

        output.Add(new AlertCandidate(
            AlertType.SlowMoving, AlertSeverity.Medium,
            $"SLOW:{drug.DrugId}",
            $"{drug.Name} {basis} {idleDays} วัน (เกณฑ์ {threshold}) คงเหลือ {total:N0} {drug.Unit}",
            DrugId: drug.DrugId));
    }
}
