using MedStockAlert.Models;
using Microsoft.EntityFrameworkCore;

namespace MedStockAlert.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Drug> Drugs => Set<Drug>();
    public DbSet<DrugBarcode> DrugBarcodes => Set<DrugBarcode>();
    public DbSet<Batch> Batches => Set<Batch>();
    public DbSet<StockTransaction> StockTransactions => Set<StockTransaction>();
    public DbSet<ReceiveDocument> ReceiveDocuments => Set<ReceiveDocument>();
    public DbSet<ImportLog> ImportLogs => Set<ImportLog>();
    public DbSet<StockPolicy> StockPolicies => Set<StockPolicy>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();

    protected override void ConfigureConventions(ModelConfigurationBuilder cb)
    {
        // เก็บ enum เป็นข้อความ อ่านง่ายใน DB และปลอดภัยเมื่อเพิ่มค่าใหม่
        cb.Properties<TransactionType>().HaveConversion<string>().HaveMaxLength(20);
        cb.Properties<BatchStatus>().HaveConversion<string>().HaveMaxLength(20);
        cb.Properties<ReceiveSource>().HaveConversion<string>().HaveMaxLength(20);
        cb.Properties<AlertType>().HaveConversion<string>().HaveMaxLength(20);
        cb.Properties<AlertSeverity>().HaveConversion<string>().HaveMaxLength(20);
        cb.Properties<AlertStatus>().HaveConversion<string>().HaveMaxLength(20);
    }

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Drug>(e =>
        {
            e.HasIndex(x => x.DrugCode).IsUnique();
            e.HasOne(x => x.Policy).WithOne(p => p.Drug).HasForeignKey<StockPolicy>(p => p.DrugId);
        });

        mb.Entity<StockPolicy>().HasKey(x => x.DrugId);

        mb.Entity<DrugBarcode>(e =>
        {
            e.HasIndex(x => x.Barcode).IsUnique();
            e.HasOne(x => x.Drug).WithMany(d => d.Barcodes).HasForeignKey(x => x.DrugId);
        });

        mb.Entity<Batch>(e =>
        {
            e.Property(x => x.UnitCost).HasPrecision(18, 2);
            e.HasIndex(x => new { x.DrugId, x.LotNo, x.ExpiryDate }).IsUnique(); // ล็อต+วันหมดอายุเดียวกัน = แถวเดียว
            e.HasIndex(x => x.ExpiryDate);
        });

        mb.Entity<StockTransaction>(e =>
        {
            e.Ignore(x => x.SignedQuantity);
            e.HasIndex(x => new { x.BatchId, x.TxDate });
            e.HasIndex(x => new { x.Type, x.TxDate });
            e.HasOne(x => x.Batch).WithMany(b => b.Transactions).HasForeignKey(x => x.BatchId);
        });

        mb.Entity<ReceiveDocument>(e => e.HasIndex(x => x.DocumentNo).IsUnique());

        mb.Entity<ImportLog>(e => e.HasIndex(x => x.FileHash).IsUnique());

        mb.Entity<Alert>(e =>
        {
            e.HasIndex(x => x.DedupeKey).IsUnique();
            e.HasIndex(x => x.Status);
        });

        mb.Entity<SystemSetting>().HasData(
            new SystemSetting { Key = "DailyDigestTime", Value = "07:30", Description = "เวลาส่งสรุปรายวัน" },
            new SystemSetting { Key = "ExpiryWarnDays", Value = "180,90,30", Description = "ระดับเตือนใกล้หมดอายุ (วัน)" },
            new SystemSetting { Key = "BlockDispenseWithinDays", Value = "30", Description = "งดจ่ายเมื่อเหลืออายุไม่เกิน (วัน)" },
            new SystemSetting { Key = "DefaultSlowMovingDays", Value = "90", Description = "เกณฑ์ยาค้างสต็อกเริ่มต้น (วัน)" },
            new SystemSetting { Key = "DefaultLeadTimeDays", Value = "7", Description = "ระยะเวลาสั่งซื้อเริ่มต้น (วัน)" },
            new SystemSetting { Key = "RepeatCriticalEveryDays", Value = "1", Description = "เตือนซ้ำเรื่องวิกฤต (วัน)" },
            new SystemSetting { Key = "RepeatNormalEveryDays", Value = "7", Description = "เตือนซ้ำเรื่องทั่วไป (วัน)" });
    }
}
