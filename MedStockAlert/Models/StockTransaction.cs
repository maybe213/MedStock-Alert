using System.ComponentModel.DataAnnotations;

namespace MedStockAlert.Models;

/// <summary>สมุดบัญชีเคลื่อนไหว: ไม่แก้ไข/ลบ ถ้าผิดให้บันทึกรายการปรับปรุง</summary>
public class StockTransaction
{
    public long Id { get; set; }
    public int BatchId { get; set; }
    public Batch Batch { get; set; } = null!;

    public TransactionType Type { get; set; }
    public int Quantity { get; set; }                      // เก็บค่าเป็นบวกเสมอ
    public DateTime TxDate { get; set; } = DateTime.UtcNow;

    public int? ReceiveDocumentId { get; set; }
    public ReceiveDocument? ReceiveDocument { get; set; }

    [MaxLength(50)]
    public string? ReferenceDoc { get; set; }              // เลขอ้างอิงใบเบิก/ใบจ่าย
    [MaxLength(500)]
    public string? Note { get; set; }
    [MaxLength(50)]
    public string? CreatedBy { get; set; }

    public int SignedQuantity => Type switch
    {
        TransactionType.Receive or TransactionType.AdjustPlus or TransactionType.Return => Quantity,
        _ => -Quantity
    };
}
