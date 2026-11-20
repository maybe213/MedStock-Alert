namespace MedStockAlert.Models;

/// <summary>ประเภทธุรกรรมในสมุดบัญชีเคลื่อนไหวสต็อก</summary>
public enum TransactionType
{
    Receive,        // รับเข้า (+)
    Dispense,       // จ่ายออก (-)
    AdjustPlus,     // ปรับปรุงเพิ่ม (+)
    AdjustMinus,    // ปรับปรุงลด (-)
    DiscardExpired, // ทำลายยาหมดอายุ (-)
    Return          // รับคืน (+)
}

public enum BatchStatus { Active, Quarantine, Expired, Depleted }

public enum ReceiveSource { Csv, Barcode, Manual }

public enum AlertType
{
    Expired,       // หมดอายุแล้ว
    ExpiringSoon,  // ใกล้หมดอายุ (ระดับ 180/90/30 ดูที่ Alert.Level)
    BelowMin,      // ต่ำกว่า Min
    ReorderPoint,  // ถึงจุดสั่งซื้อ
    OverMax,       // เกิน Max
    SlowMoving     // ค้างสต็อกนาน
}

public enum AlertSeverity { Info, Low, Medium, High, Critical }

public enum AlertStatus { Open, Resolved, Snoozed }
