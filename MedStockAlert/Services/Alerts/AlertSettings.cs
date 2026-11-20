namespace MedStockAlert.Services.Alerts;

/// <summary>ค่าตั้งค่ากลางที่ใช้ตัดสินการแจ้งเตือน (โหลดจากตาราง SystemSettings)</summary>
public sealed record AlertSettings
{
    /// <summary>ระดับเตือนใกล้หมดอายุ (วัน) เรียงจากน้อยไปมาก เช่น 30, 90, 180</summary>
    public IReadOnlyList<int> ExpiryWarnDays { get; init; } = new[] { 30, 90, 180 };
    public int DefaultSlowMovingDays { get; init; } = 90;
    public int DefaultLeadTimeDays { get; init; } = 7;
    /// <summary>รอบเตือนซ้ำ (วัน) สำหรับเรื่องระดับ High ขึ้นไป</summary>
    public int RepeatCriticalEveryDays { get; init; } = 1;
    /// <summary>รอบเตือนซ้ำ (วัน) สำหรับเรื่องระดับต่ำกว่า High</summary>
    public int RepeatNormalEveryDays { get; init; } = 7;
    /// <summary>ยาที่เหลืออายุไม่เกินกี่วันถือว่า "งดจ่าย" และไม่นับเป็นสต็อกที่ใช้ได้</summary>
    public int BlockDispenseWithinDays { get; init; } = 30;

    public static AlertSettings FromDictionary(IReadOnlyDictionary<string, string> values)
    {
        var d = new AlertSettings();
        int Int(string key, int fallback) =>
            values.TryGetValue(key, out var v) && int.TryParse(v, out var n) ? n : fallback;

        return d with
        {
            ExpiryWarnDays = values.TryGetValue("ExpiryWarnDays", out var days)
                ? ParseDays(days, d.ExpiryWarnDays) : d.ExpiryWarnDays,
            DefaultSlowMovingDays = Int("DefaultSlowMovingDays", d.DefaultSlowMovingDays),
            DefaultLeadTimeDays = Int("DefaultLeadTimeDays", d.DefaultLeadTimeDays),
            RepeatCriticalEveryDays = Int("RepeatCriticalEveryDays", d.RepeatCriticalEveryDays),
            RepeatNormalEveryDays = Int("RepeatNormalEveryDays", d.RepeatNormalEveryDays),
            BlockDispenseWithinDays = Int("BlockDispenseWithinDays", d.BlockDispenseWithinDays),
        };
    }

    /// <summary>แปลงข้อความ "180,90,30" เป็นรายการวันที่เรียงน้อยไปมาก (ตัดค่าที่ไม่ใช่ตัวเลข/≤0/ซ้ำ)</summary>
    public static IReadOnlyList<int> ParseDays(string? text, IReadOnlyList<int> fallback)
    {
        if (string.IsNullOrWhiteSpace(text)) return fallback;
        var list = text.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                       .Select(s => int.TryParse(s, out var n) ? n : 0)
                       .Where(n => n > 0)
                       .Distinct()
                       .OrderBy(n => n)
                       .ToList();
        return list.Count == 0 ? fallback : list;
    }
}
