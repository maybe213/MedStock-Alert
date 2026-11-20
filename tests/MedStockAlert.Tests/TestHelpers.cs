using MedStockAlert.Models;
using MedStockAlert.Services.Alerts;

namespace MedStockAlert.Tests;

internal static class T
{
    public static readonly DateOnly Today = new(2026, 11, 20);

    public static BatchSnapshot Batch(int id, int daysToExpiry, int qty,
        int receivedDaysAgo = 10, BatchStatus status = BatchStatus.Active) =>
        new(id, "L" + id, Today.AddDays(daysToExpiry), Today.AddDays(-receivedDaysAgo), qty, status);

    public static DrugStockSnapshot Drug(int id, IEnumerable<BatchSnapshot> batches,
        PolicySnapshot? policy = null, decimal avgDailyUsage = 0, int? lastDispensedDaysAgo = null) =>
        new()
        {
            DrugId = id,
            DrugCode = "MED" + id,
            Name = "ยา" + id,
            Unit = "เม็ด",
            Batches = batches.ToList(),
            Policy = policy,
            AvgDailyUsage = avgDailyUsage,
            LastDispensedDate = lastDispensedDaysAgo.HasValue ? Today.AddDays(-lastDispensedDaysAgo.Value) : null
        };

    public static List<AlertCandidate> Run(params DrugStockSnapshot[] drugs) =>
        AlertEngine.Evaluate(drugs, new AlertSettings(), Today);
}
