using System.Text;
using MedStockAlert.Models;

namespace MedStockAlert.Services.Alerts;

/// <summary>สร้างข้อความสรุปรายวันสำหรับส่งเข้า LINE (ข้อความเดียว จัดกลุ่มตามความเร่งด่วน)</summary>
public static class DigestBuilder
{
    private const int LineTextLimit = 4800; // LINE จำกัด 5,000 ตัวอักษรต่อข้อความ

    public static string Build(IEnumerable<Alert> alerts, DateOnly today, int maxPerGroup = 8)
    {
        var list = alerts.ToList();
        var sb = new StringBuilder();
        sb.AppendLine($"📋 สรุปคลังยา OPD {ThaiDate.Format(today)}");

        if (list.Count == 0)
        {
            sb.AppendLine();
            sb.Append("✅ ไม่มีรายการที่ต้องดำเนินการ");
            return sb.ToString();
        }

        foreach (var (title, items) in Groups(list))
        {
            sb.AppendLine();
            sb.AppendLine($"{title} ({items.Count})");
            foreach (var a in items.Take(maxPerGroup))
                sb.AppendLine($" • {a.Message}");
            if (items.Count > maxPerGroup)
                sb.AppendLine($" … และอีก {items.Count - maxPerGroup} รายการ");
        }

        var text = sb.ToString().TrimEnd();
        return text.Length <= LineTextLimit
            ? text
            : text[..LineTextLimit] + "\n…(ข้อความถูกตัด ดูรายการทั้งหมดในระบบ)";
    }

    private static IEnumerable<(string Title, List<Alert> Items)> Groups(List<Alert> alerts)
    {
        var expired = alerts.Where(a => a.Type == AlertType.Expired).ToList();
        if (expired.Count > 0) yield return ("🔴 หมดอายุแล้ว", expired);

        var tiers = alerts.Where(a => a.Type == AlertType.ExpiringSoon)
                          .GroupBy(a => a.Level ?? int.MaxValue)
                          .OrderBy(g => g.Key)
                          .ToList();
        for (var i = 0; i < tiers.Count; i++)
        {
            var icon = i == 0 ? "🔴" : i == 1 ? "🟡" : "🟢";
            var level = tiers[i].Key == int.MaxValue ? "" : $" ≤{tiers[i].Key} วัน";
            yield return ($"{icon} ใกล้หมดอายุ{level}", tiers[i].ToList());
        }

        var belowMin = alerts.Where(a => a.Type == AlertType.BelowMin).ToList();
        if (belowMin.Count > 0) yield return ("🔴 ต่ำกว่าจุดต่ำสุด (Min)", belowMin);

        var reorder = alerts.Where(a => a.Type == AlertType.ReorderPoint).ToList();
        if (reorder.Count > 0) yield return ("🟡 ถึงจุดสั่งซื้อ", reorder);

        var slow = alerts.Where(a => a.Type == AlertType.SlowMoving).ToList();
        if (slow.Count > 0) yield return ("🟡 ยาค้างสต็อก", slow);

        var over = alerts.Where(a => a.Type == AlertType.OverMax).ToList();
        if (over.Count > 0) yield return ("🔵 เกินจุดสูงสุด (Max)", over);
    }
}
