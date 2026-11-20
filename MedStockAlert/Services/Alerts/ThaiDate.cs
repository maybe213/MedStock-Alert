namespace MedStockAlert.Services.Alerts;

public static class ThaiDate
{
    /// <summary>รูปแบบ dd/MM/yyyy ปี พ.ศ.</summary>
    public static string Format(DateOnly d) => $"{d.Day:00}/{d.Month:00}/{d.Year + 543}";
}
