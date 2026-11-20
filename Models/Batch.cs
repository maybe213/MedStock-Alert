using System.ComponentModel.DataAnnotations;

namespace MedStockAlert.Models;

/// <summary>ล็อตยา: 1 ยามีได้หลายล็อต แต่ละล็อตมีวันหมดอายุของตัวเอง</summary>
public class Batch
{
    public int Id { get; set; }
    public int DrugId { get; set; }
    public Drug Drug { get; set; } = null!;

    [MaxLength(50)]
    public string LotNo { get; set; } = string.Empty;
    public DateOnly ExpiryDate { get; set; }
    public DateOnly ReceivedDate { get; set; }
    public decimal UnitCost { get; set; }
    public BatchStatus Status { get; set; } = BatchStatus.Active;

    public ICollection<StockTransaction> Transactions { get; set; } = new List<StockTransaction>();

    /// <summary>ยอดคงเหลือ = ผลรวมธุรกรรม (ต้อง Include Transactions ก่อนเรียก)</summary>
    public int GetQuantityOnHand() => Transactions.Sum(t => t.SignedQuantity);

    /// <summary>วันที่จ่ายล่าสุด ใช้ตรวจยาค้างสต็อก (ถ้าไม่เคยจ่ายคืน null)</summary>
    public DateTime? GetLastDispensedAt() =>
        Transactions.Where(t => t.Type == TransactionType.Dispense)
                    .Select(t => (DateTime?)t.TxDate).Max();
}
