# MedStock Alert

ระบบบริหารคลังยาและแจ้งเตือนอัตโนมัติผ่าน LINE สำหรับคลังยา OPD โรงพยาบาลสุราษฎร์ธานี
โปรเจกต์ ASP.NET Core 8 MVC โปรเจกต์เดียว (`MedStockAlert/`) + โปรเจกต์ทดสอบ xUnit (`tests/MedStockAlert.Tests/`)

## เริ่มใช้งาน

```powershell
dotnet build
dotnet test
# สร้างฐานข้อมูล (ต้องมี SQL Server / SQLEXPRESS ตาม ConnectionStrings:Default)
dotnet ef migrations add InitialCreate --project MedStockAlert
dotnet ef database update --project MedStockAlert
dotnet run --project MedStockAlert
```

## โครงสร้าง

- `Models/` Entity + Enums
- `Data/AppDbContext.cs`
- `Services/Alerts/` AlertEngine, AlertReconciler, DigestBuilder, AlertSettings, Snapshots, ThaiDate
- `Controllers/`, `Views/`, `wwwroot/`, `Filters/`, `Migrations/`
- `docs/sample-import.csv` ตัวอย่างไฟล์นำเข้า (ร่าง)

ยังไม่มี: LINE client, Scheduler, นำเข้า CSV, บาร์โค้ด, mapper จาก Entity เป็น `DrugStockSnapshot`
