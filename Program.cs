using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MedStockAlert.Data;
using MedStockAlert.Filters;
using MedStockAlert.Services;

var builder = WebApplication.CreateBuilder(args);

// -------------------- Services --------------------
builder.Services.AddControllersWithViews(options =>
{
    // บังคับล็อกอินทุกหน้าโดยค่าเริ่มต้น (ยกเว้นหน้าที่ใส่ [AllowAnonymous])
    options.Filters.Add(new AuthorizeFilter());
});

// Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);          // กะทำงานปกติ
        options.SlidingExpiration = true;
        options.Cookie.Name = "MedStockAlert.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

builder.Services.AddAuthorization(options =>
{
    // Policy ตัวอย่าง (ใช้ในอนาคตถ้ามีหลายบทบาท)
    options.AddPolicy("PharmacistOnly", policy =>
        policy.RequireRole("Pharmacist", "Admin"));
});

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// Application services
builder.Services.AddScoped<IAuthService, AuthService>();

// -------------------- App --------------------
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
