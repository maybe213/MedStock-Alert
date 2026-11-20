using System.ComponentModel.DataAnnotations;

namespace MedStockAlert.Models;

/// <summary>บันทึกการนำเข้าไฟล์ CSV (ใช้ FileHash ป้องกันนำเข้าซ้ำ)</summary>
public class ImportLog
{
    public int Id { get; set; }
    [MaxLength(260)]
    public string FileName { get; set; } = string.Empty;
    [MaxLength(64)]
    public string FileHash { get; set; } = string.Empty;   // SHA-256
    public int RowsTotal { get; set; }
    public int RowsOk { get; set; }
    public int RowsError { get; set; }
    public string? ErrorDetail { get; set; }
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
}
