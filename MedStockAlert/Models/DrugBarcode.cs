using System.ComponentModel.DataAnnotations;

namespace MedStockAlert.Models;

/// <summary>ผูกบาร์โค้ดกับยา (1 ยามีได้หลายบาร์โค้ด เช่น คนละบรรจุภัณฑ์)</summary>
public class DrugBarcode
{
    public int Id { get; set; }
    public int DrugId { get; set; }
    public Drug Drug { get; set; } = null!;

    [MaxLength(64)]
    public string Barcode { get; set; } = string.Empty;    // GTIN / EAN-13 / Code128
    [MaxLength(20)]
    public string? BarcodeType { get; set; }               // EAN13, CODE128, GS1_DATAMATRIX
}
