using MedStockAlert.Models;
using MedStockAlert.Services.Alerts;
using Xunit;

namespace MedStockAlert.Tests;

public class AlertReconcilerTests
{
    private static readonly DateTime Now = new(2026, 11, 20, 0, 30, 0, DateTimeKind.Utc);
    private static readonly AlertSettings Settings = new();

    private static AlertCandidate Cand(string key, AlertSeverity sev = AlertSeverity.Medium) =>
        new(AlertType.SlowMoving, sev, key, "msg " + key, DrugId: 1);

    private static Alert Existing(string key, AlertSeverity sev = AlertSeverity.Medium,
        AlertStatus status = AlertStatus.Open, DateTime? lastNotified = null) =>
        new()
        {
            DedupeKey = key, Severity = sev, Status = status, LastNotifiedAt = lastNotified,
            Type = AlertType.SlowMoving, Message = "old", FirstSeenAt = Now.AddDays(-30)
        };

    private static ReconcileResult Run(IEnumerable<Alert> existing, params AlertCandidate[] candidates) =>
        AlertReconciler.Reconcile(existing, candidates, Settings, Now);

    [Fact]
    public void New_candidate_creates_alert_and_notifies()
    {
        var r = Run(Array.Empty<Alert>(), Cand("A"));
        var created = Assert.Single(r.Created);
        Assert.Equal(AlertStatus.Open, created.Status);
        Assert.Single(r.ToNotify);
        Assert.Empty(r.Resolved);
    }

    [Fact]
    public void Duplicate_candidates_with_same_key_create_one_alert()
    {
        var r = Run(Array.Empty<Alert>(), Cand("A"), Cand("A"));
        Assert.Single(r.Created);
    }

    [Fact]
    public void High_severity_repeats_daily()
    {
        var yesterday = Existing("A", AlertSeverity.High, lastNotified: Now.AddDays(-1).AddMinutes(5)); // เกือบ 24 ชม. แต่คนละวัน
        Assert.Single(Run(new[] { yesterday }, Cand("A", AlertSeverity.High)).ToNotify);
    }

    [Fact]
    public void Already_notified_today_is_not_repeated()
    {
        var today = Existing("A", AlertSeverity.High, lastNotified: Now.AddMinutes(-10));
        Assert.Empty(Run(new[] { today }, Cand("A", AlertSeverity.High)).ToNotify);
    }

    [Theory]
    [InlineData(3, false)]
    [InlineData(6, false)]
    [InlineData(7, true)]
    public void Normal_severity_repeats_weekly(int daysAgo, bool expectNotify)
    {
        var a = Existing("A", AlertSeverity.Medium, lastNotified: Now.AddDays(-daysAgo));
        Assert.Equal(expectNotify, Run(new[] { a }, Cand("A")).ToNotify.Count == 1);
    }

    [Fact]
    public void Open_alert_with_no_matching_candidate_is_resolved()
    {
        var a = Existing("A");
        var r = Run(new[] { a });
        Assert.Single(r.Resolved);
        Assert.Equal(AlertStatus.Resolved, a.Status);
        Assert.Equal(Now, a.ResolvedAt);
    }

    [Fact]
    public void Resolved_alert_that_recurs_is_reopened_and_notified()
    {
        var a = Existing("A", status: AlertStatus.Resolved, lastNotified: Now.AddDays(-2));
        var r = Run(new[] { a }, Cand("A"));
        Assert.Single(r.Reopened);
        Assert.Single(r.ToNotify);
        Assert.Equal(AlertStatus.Open, a.Status);
        Assert.Null(a.LastNotifiedAt);
        Assert.Empty(r.Created); // ใช้แถวเดิม (DedupeKey เป็น unique)
    }

    [Fact]
    public void Already_resolved_alert_stays_resolved_when_gone()
    {
        var a = Existing("A", status: AlertStatus.Resolved);
        var r = Run(new[] { a });
        Assert.Empty(r.Resolved);
    }

    [Fact]
    public void Snoozed_alert_is_silent_until_snooze_expires()
    {
        var snoozed = Existing("A", status: AlertStatus.Snoozed);
        snoozed.SnoozedUntil = Now.AddDays(2);
        var r = Run(new[] { snoozed }, Cand("A"));
        Assert.Empty(r.ToNotify);
        Assert.Equal(AlertStatus.Snoozed, snoozed.Status);

        var expired = Existing("B", status: AlertStatus.Snoozed);
        expired.SnoozedUntil = Now.AddHours(-1);
        var r2 = Run(new[] { expired }, Cand("B"));
        Assert.Single(r2.ToNotify);
        Assert.Equal(AlertStatus.Open, expired.Status);
    }

    [Fact]
    public void Message_is_refreshed_on_existing_alert()
    {
        var a = Existing("A", lastNotified: Now.AddMinutes(-5));
        Run(new[] { a }, Cand("A"));
        Assert.Equal("msg A", a.Message);
    }
}

public class DigestBuilderTests
{
    private static Alert A(AlertType type, string msg, int? level = null) =>
        new() { Type = type, Message = msg, Level = level, DedupeKey = Guid.NewGuid().ToString() };

    [Fact]
    public void Empty_digest_says_all_clear()
    {
        var text = DigestBuilder.Build(Array.Empty<Alert>(), T.Today);
        Assert.Contains("ไม่มีรายการที่ต้องดำเนินการ", text);
        Assert.Contains("20/11/2569", text); // ปี พ.ศ.
    }

    [Fact]
    public void Groups_are_ordered_by_urgency()
    {
        var text = DigestBuilder.Build(new[]
        {
            A(AlertType.SlowMoving, "slow"),
            A(AlertType.ExpiringSoon, "e90", 90),
            A(AlertType.Expired, "exp"),
            A(AlertType.ExpiringSoon, "e30", 30),
            A(AlertType.BelowMin, "min"),
        }, T.Today);

        int Pos(string s) => text.IndexOf(s, StringComparison.Ordinal);
        Assert.True(Pos("หมดอายุแล้ว") < Pos("≤30 วัน"));
        Assert.True(Pos("≤30 วัน") < Pos("≤90 วัน"));
        Assert.True(Pos("≤90 วัน") < Pos("ต่ำกว่าจุดต่ำสุด"));
        Assert.True(Pos("ต่ำกว่าจุดต่ำสุด") < Pos("ยาค้างสต็อก"));
    }

    [Fact]
    public void Long_groups_are_truncated_with_remaining_count()
    {
        var many = Enumerable.Range(1, 12).Select(i => A(AlertType.Expired, "item" + i));
        var text = DigestBuilder.Build(many, T.Today, maxPerGroup: 8);
        Assert.Contains("item8", text);
        Assert.DoesNotContain("item9", text);
        Assert.Contains("อีก 4 รายการ", text);
    }

    [Fact]
    public void Digest_never_exceeds_line_text_limit()
    {
        var many = Enumerable.Range(1, 500).Select(i => A(AlertType.Expired, new string('ก', 400)));
        var text = DigestBuilder.Build(many, T.Today, maxPerGroup: 500);
        Assert.True(text.Length <= 5000);
    }
}
