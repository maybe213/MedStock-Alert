namespace MedStockAlert.Models;

/// <summary>เกณฑ์สต็อกรายยา: ค่า null = ใช้ค่ากลางจาก SystemSetting</summary>
public class StockPolicy
{
    public int DrugId { get; set; }                        // PK และ FK
    public Drug Drug { get; set; } = null!;

    public int MinQty { get; set; }
    public int MaxQty { get; set; }

    /// <summary>true = คำนวณ ROP อัตโนมัติ = (อัตราจ่าย/วัน × LeadTime) + Min</summary>
    public bool UseAutoReorderPoint { get; set; } = true;
    public int? ManualReorderPoint { get; set; }

    public int? LeadTimeDays { get; set; }
    public int? SlowMovingDays { get; set; }               // เกณฑ์ยาค้างสต็อกรายยา
    public string? ExpiryWarnDays { get; set; }            // เช่น "180,90,30"
}
