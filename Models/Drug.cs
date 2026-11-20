using System.ComponentModel.DataAnnotations;

namespace MedStockAlert.Models;

/// <summary>ข้อมูลหลักของยา/เวชภัณฑ์ (Master)</summary>
public class Drug
{
    public int Id { get; set; }

    [MaxLength(30)]
    public string DrugCode { get; set; } = string.Empty;   // รหัสยาภายใน (รอยืนยันรูปแบบจากเจ้าหน้าที่)

    [MaxLength(200)]
    public string GenericName { get; set; } = string.Empty;
    [MaxLength(200)]
    public string? TradeName { get; set; }
    [MaxLength(50)]
    public string? Strength { get; set; }                  // เช่น 500 mg
    [MaxLength(50)]
    public string? DosageForm { get; set; }                // tab, cap, inj, syr
    [MaxLength(30)]
    public string Unit { get; set; } = string.Empty;       // หน่วยนับ
    [MaxLength(100)]
    public string? Category { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public StockPolicy? Policy { get; set; }
    public ICollection<DrugBarcode> Barcodes { get; set; } = new List<DrugBarcode>();
    public ICollection<Batch> Batches { get; set; } = new List<Batch>();
}
