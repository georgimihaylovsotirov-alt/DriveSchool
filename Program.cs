using System.Globalization;
using DriveSchool.Data;
using DriveSchool.Models;
using DriveSchool.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var sqlServerConnection = builder.Configuration["SQLSERVER_CONNECTION_STRING"]
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddSingleton(new DatabaseAvailability(!string.IsNullOrWhiteSpace(sqlServerConnection)));
builder.Services.AddDbContext<DriveSchoolDbContext>(options =>
{
    if (!string.IsNullOrWhiteSpace(sqlServerConnection))
    {
        options.UseSqlServer(sqlServerConnection);
    }
});
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 10;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<DriveSchoolDbContext>()
    .AddDefaultTokenProviders()
    .AddErrorDescriber<BulgarianIdentityErrorDescriber>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.Name = "DriveSchool.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.SlidingExpiration = true;
});
builder.Services.AddControllersWithViews();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = "DriveSchool.Session";
    options.Cookie.HttpOnly = true;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});
builder.Services.AddScoped<BookingService>();

var app = builder.Build();
var bgCulture = CultureInfo.GetCultureInfo("bg-BG");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(bgCulture),
    SupportedCultures = [bgCulture],
    SupportedUICultures = [bgCulture]
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

if (!string.IsNullOrWhiteSpace(sqlServerConnection))
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<DriveSchoolDbContext>();
    await db.Database.MigrateAsync();
    await SeedData.EnsureRolesAsync(scope.ServiceProvider);
    await SeedData.InitializeOwnerIfConfiguredAsync(scope.ServiceProvider, builder.Configuration);
    if (app.Environment.IsDevelopment())
    {
        await SeedData.InitializeAsync(scope.ServiceProvider);
    }
}

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
