using MedStockAlert.Models;
using MedStockAlert.Services.Alerts;
using Xunit;

namespace MedStockAlert.Tests;

public class ExpiryRuleTests
{
    [Fact]
    public void Expired_batch_is_critical()
    {
        var r = T.Run(T.Drug(1, new[] { T.Batch(1, -1, 200) }));
        var a = Assert.Single(r);
        Assert.Equal(AlertType.Expired, a.Type);
        Assert.Equal(AlertSeverity.Critical, a.Severity);
        Assert.Equal("EXPIRED:1", a.DedupeKey);
    }

    [Theory]
    [InlineData(25, 30, AlertSeverity.High)]
    [InlineData(30, 30, AlertSeverity.High)]   // ขอบเขต: เหลือ 30 วันพอดี
    [InlineData(60, 90, AlertSeverity.Medium)]
    [InlineData(150, 180, AlertSeverity.Low)]
    public void Expiring_batch_uses_smallest_matching_tier(int days, int expectedTier, AlertSeverity expectedSeverity)
    {
        var r = T.Run(T.Drug(1, new[] { T.Batch(1, days, 100) }));
        var a = Assert.Single(r);
        Assert.Equal(AlertType.ExpiringSoon, a.Type);
        Assert.Equal(expectedTier, a.Level);
        Assert.Equal(expectedSeverity, a.Severity);
        Assert.Equal($"EXPIRY:{expectedTier}:1", a.DedupeKey);
    }

    [Fact]
    public void Batch_beyond_all_tiers_has_no_alert()
    {
        Assert.Empty(T.Run(T.Drug(1, new[] { T.Batch(1, 400, 100) })));
    }

    [Fact]
    public void Expiring_today_is_not_yet_expired()
    {
        var a = Assert.Single(T.Run(T.Drug(1, new[] { T.Batch(1, 0, 100) })));
        Assert.Equal(AlertType.ExpiringSoon, a.Type);
    }

    [Fact]
    public void Empty_or_depleted_batches_are_ignored()
    {
        var r = T.Run(T.Drug(1, new[]
        {
            T.Batch(1, -5, 0),
            T.Batch(2, 10, 50, status: BatchStatus.Depleted)
        }));
        Assert.Empty(r);
    }

    [Fact]
    public void Per_drug_expiry_tiers_override_global()
    {
        var policy = new PolicySnapshot(ExpiryWarnDays: "60");
        var r = T.Run(T.Drug(1, new[] { T.Batch(1, 50, 100) }, policy));
        var a = Assert.Single(r);
        Assert.Equal(60, a.Level);
    }
}

public class StockLevelRuleTests
{
    private static BatchSnapshot Healthy(int qty) => T.Batch(1, 300, qty);

    [Fact]
    public void At_or_below_min_is_high_severity()
    {
        var policy = new PolicySnapshot(MinQty: 50, MaxQty: 500);
        var a = Assert.Single(T.Run(T.Drug(1, new[] { Healthy(50) }, policy)));
        Assert.Equal(AlertType.BelowMin, a.Type);
        Assert.Equal(AlertSeverity.High, a.Severity);
        Assert.Contains("450", a.Message); // แนะนำสั่ง = Max - คงเหลือ
    }

    [Fact]
    public void Auto_reorder_point_uses_usage_times_leadtime_plus_min()
    {
        // ROP = ceil(10 * 7) + 50 = 120
        var policy = new PolicySnapshot(MinQty: 50, MaxQty: 500);
        var drug = T.Drug(1, new[] { Healthy(100) }, policy, avgDailyUsage: 10);
        Assert.Equal(120, AlertEngine.ReorderPoint(drug, new AlertSettings()));

        var a = Assert.Single(T.Run(drug));
        Assert.Equal(AlertType.ReorderPoint, a.Type);
        Assert.Contains("400", a.Message);
    }

    [Fact]
    public void Stock_above_reorder_point_has_no_alert()
    {
        var policy = new PolicySnapshot(MinQty: 50, MaxQty: 500);
        Assert.Empty(T.Run(T.Drug(1, new[] { Healthy(200) }, policy, avgDailyUsage: 10)));
    }

    [Fact]
    public void Manual_reorder_point_overrides_auto()
    {
        var below = new PolicySnapshot(MinQty: 10, MaxQty: 500, UseAutoReorderPoint: false, ManualReorderPoint: 50);
        var a = Assert.Single(T.Run(T.Drug(1, new[] { Healthy(40) }, below, avgDailyUsage: 100)));
        Assert.Equal(AlertType.ReorderPoint, a.Type);

        var above = new PolicySnapshot(MinQty: 10, MaxQty: 500, UseAutoReorderPoint: false, ManualReorderPoint: 30);
        Assert.Empty(T.Run(T.Drug(1, new[] { Healthy(40) }, above, avgDailyUsage: 100)));
    }

    [Fact]
    public void Over_max_is_info_only()
    {
        var policy = new PolicySnapshot(MinQty: 10, MaxQty: 100);
        var a = Assert.Single(T.Run(T.Drug(1, new[] { Healthy(150) }, policy)));
        Assert.Equal(AlertType.OverMax, a.Type);
        Assert.Equal(AlertSeverity.Info, a.Severity);
    }

    [Fact]
    public void Batches_in_dispense_block_window_do_not_count_as_usable_stock()
    {
        // ล็อตเหลืออายุ 20 วัน (งดจ่าย) 500 เม็ด + ล็อตดี 50 เม็ด -> ใช้ได้แค่ 50 ต่ำกว่า Min 100
        var policy = new PolicySnapshot(MinQty: 100, MaxQty: 1000);
        var r = T.Run(T.Drug(1, new[] { T.Batch(1, 20, 500), T.Batch(2, 200, 50) }, policy));
        Assert.Contains(r, x => x.Type == AlertType.BelowMin);
        Assert.Contains(r, x => x.Type == AlertType.ExpiringSoon); // และเตือนใกล้หมดอายุของล็อตแรกด้วย
    }

    [Fact]
    public void Quarantined_batches_do_not_count_as_usable_stock()
    {
        var policy = new PolicySnapshot(MinQty: 100, MaxQty: 1000);
        var r = T.Run(T.Drug(1, new[] { T.Batch(1, 300, 500, status: BatchStatus.Quarantine) }, policy));
        Assert.Contains(r, x => x.Type == AlertType.BelowMin);
    }

    [Fact]
    public void Drug_without_policy_has_no_stock_level_alerts()
    {
        Assert.Empty(T.Run(T.Drug(1, new[] { Healthy(1) })));
    }

    [Fact]
    public void Unconfigured_zero_policy_does_not_alert_every_empty_drug()
    {
        Assert.Empty(T.Run(T.Drug(1, new[] { Healthy(0) }, new PolicySnapshot())));
    }
}

public class SlowMovingRuleTests
{
    private static BatchSnapshot Stock(int receivedDaysAgo) => T.Batch(1, 300, 100, receivedDaysAgo);

    [Theory]
    [InlineData(90, true)]   // ครบเกณฑ์พอดี
    [InlineData(89, false)]
    [InlineData(200, true)]
    public void Uses_last_dispensed_date(int lastDispensedDaysAgo, bool expectAlert)
    {
        var r = T.Run(T.Drug(1, new[] { Stock(300) }, lastDispensedDaysAgo: lastDispensedDaysAgo));
        Assert.Equal(expectAlert, r.Any(x => x.Type == AlertType.SlowMoving));
    }

    [Fact]
    public void Never_dispensed_counts_from_received_date()
    {
        Assert.Contains(T.Run(T.Drug(1, new[] { Stock(100) })), x => x.Type == AlertType.SlowMoving);
        Assert.DoesNotContain(T.Run(T.Drug(1, new[] { Stock(30) })), x => x.Type == AlertType.SlowMoving);
    }

    [Fact]
    public void Recent_receipt_resets_the_clock_even_if_last_dispense_is_old()
    {
        var r = T.Run(T.Drug(1, new[] { Stock(5) }, lastDispensedDaysAgo: 200));
        Assert.DoesNotContain(r, x => x.Type == AlertType.SlowMoving);
    }

    [Fact]
    public void Per_drug_threshold_overrides_default()
    {
        var policy = new PolicySnapshot(SlowMovingDays: 30);
        var r = T.Run(T.Drug(1, new[] { Stock(300) }, policy, lastDispensedDaysAgo: 40));
        Assert.Contains(r, x => x.Type == AlertType.SlowMoving);
    }

    [Fact]
    public void Drug_with_no_stock_is_not_slow_moving()
    {
        var r = T.Run(T.Drug(1, new[] { T.Batch(1, 300, 0, 300) }, lastDispensedDaysAgo: 300));
        Assert.DoesNotContain(r, x => x.Type == AlertType.SlowMoving);
    }
}

public class AlertSettingsTests
{
    [Fact]
    public void ParseDays_sorts_dedupes_and_drops_invalid()
    {
        var r = AlertSettings.ParseDays("180, 90,30,abc,-5,90", new[] { 1 });
        Assert.Equal(new[] { 30, 90, 180 }, r);
    }

    [Fact]
    public void ParseDays_falls_back_when_empty_or_invalid()
    {
        Assert.Equal(new[] { 7 }, AlertSettings.ParseDays("", new[] { 7 }));
        Assert.Equal(new[] { 7 }, AlertSettings.ParseDays("x,y", new[] { 7 }));
    }

    [Fact]
    public void FromDictionary_reads_values_and_keeps_defaults_for_missing()
    {
        var s = AlertSettings.FromDictionary(new Dictionary<string, string>
        {
            ["ExpiryWarnDays"] = "180,90,30",
            ["DefaultSlowMovingDays"] = "120"
        });
        Assert.Equal(new[] { 30, 90, 180 }, s.ExpiryWarnDays);
        Assert.Equal(120, s.DefaultSlowMovingDays);
        Assert.Equal(7, s.DefaultLeadTimeDays); // ไม่ได้ระบุ ใช้ค่าเริ่มต้น
    }
}
