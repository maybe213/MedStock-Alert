using System.ComponentModel.DataAnnotations;

namespace MedStockAlert.Models;

/// <summary>เอกสารรับยาเข้า (หัวเอกสาร)</summary>
public class ReceiveDocument
{
    public int Id { get; set; }
    [MaxLength(30)]
    public string DocumentNo { get; set; } = string.Empty; // RCV-yyyyMMdd-###
    [MaxLength(50)]
    public string? InvoiceNo { get; set; }
    [MaxLength(150)]
    public string? Supplier { get; set; }
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public ReceiveSource Source { get; set; }

    public int? ImportLogId { get; set; }
    public ImportLog? ImportLog { get; set; }

    public ICollection<StockTransaction> Transactions { get; set; } = new List<StockTransaction>();
}
