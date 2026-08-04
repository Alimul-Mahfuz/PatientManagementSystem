using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PatientManagementSystem.Data;
using PatientManagementSystem.Data.Seeders;
using PatientManagementSystem.Models.Entities;
using PatientManagementSystem.Services;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// QuestPDF community license (free for open-source / non-commercial projects).
QuestPDF.Settings.License = LicenseType.Community;

// EF Core DbContext.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<SidebarProviderService>();

// Identity (cookies; UI views not used, we serve our own Login.cshtml).

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireUppercase = true;
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedEmail = false;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();


builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Authentication/Login";
    options.AccessDeniedPath = "/Authentication/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Seed roles + admin user on startup (dev only; gate behind env if needed).
using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;
    try
    {
        var db = sp.GetRequiredService<ApplicationDbContext>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
        var logger = sp.GetRequiredService<ILogger<Program>>();
        await UserRoleSeeder.SeedAsync(userManager, roleManager, logger);

        // Stable-ID permissions catalog (idempotent; never re-IDs existing rows).
        await PermissionSeeder.SeedAsync(db, logger);

        // Grant every catalog permission to the Admin role (idempotent).
        await AdminRolePermissionSeeder.SeedAsync(db, roleManager, logger);
    }
    catch (Exception ex)
    {
        var logger = sp.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding roles/users.");
    }
}


app.Run();