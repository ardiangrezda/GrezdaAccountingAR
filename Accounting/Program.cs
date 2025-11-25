using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Accounting.Data;
using Accounting.Models;
using Accounting.Services;

var builder = WebApplication.CreateBuilder(args);

// read provider (can be overridden by environment variable)
var provider = builder.Configuration.GetValue<string>("DatabaseProvider") ?? "SqlServer";

// Register DbContextFactory instead of DbContext (for Blazor Server thread-safety)
if (provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
        options.UseSqlite(builder.Configuration.GetConnectionString("SqliteConnection")));
}
else
{
    builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
}

// Add Identity services - Identity still needs a scoped DbContext
builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
{
    var connectionString = provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase)
        ? builder.Configuration.GetConnectionString("SqliteConnection")
        : builder.Configuration.GetConnectionString("DefaultConnection");
    
    if (provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlite(connectionString);
    }
    else
    {
        options.UseSqlServer(connectionString);
    }
});

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
    options.LogoutPath = "/login";
    options.AccessDeniedPath = "/login";
    options.Cookie.Name = ".AspNetCore.Identity.Application";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(1);
});

// Blazor
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor()
    .AddHubOptions(options =>
    {
        options.MaximumReceiveMessageSize = 32 * 1024 * 1024; // 32MB
    });

builder.Services.Configure<CircuitOptions>(options =>
{
    options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(3);
    options.JSInteropDefaultCallTimeout = TimeSpan.FromMinutes(1);
});

builder.Services.AddScoped<ArticleService>();
builder.Services.AddScoped<ILocalizationService, LocalizationService>();
builder.Services.AddScoped<SubjectService>();
builder.Services.AddScoped<SalesService>();
builder.Services.AddScoped<SalesCategoryService>();
builder.Services.AddScoped<BusinessUnitService>();
builder.Services.AddScoped<BusinessUnitStateContainer>();
builder.Services.AddSingleton<StateContainer>();
builder.Services.AddScoped<InvoiceNumberService>();
builder.Services.AddScoped<CompanySettingsService>();

// Keep UserModuleAccessService scoped, it will create short-lived contexts via IServiceScopeFactory
builder.Services.AddScoped<UserModuleAccessService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();

// If using SQLite, set busy timeout at runtime (5000 ms) via PRAGMA so concurrent small-contention writes don't fail immediately.
// This must run after the app's service provider is built.
if (provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
{
    try
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var conn = db.Database.GetDbConnection();
        try
        {
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "PRAGMA busy_timeout = 5000;";
            cmd.ExecuteNonQuery();
        }
        finally
        {
            // keep connection closed; EF will open it when needed
            conn.Close();
        }
    }
    catch
    {
        // swallow any PRAGMA failure – PRAGMA is best-effort and not critical for schema creation via EF tools
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value?.ToLower() ?? "";
    var isAuthenticated = context.User?.Identity?.IsAuthenticated == true;
    
    if (path.StartsWith("/login") || 
        path.StartsWith("/account/login") ||
        path.StartsWith("/_framework") || 
        path.StartsWith("/_blazor") ||
        path.StartsWith("/css") || 
        path.StartsWith("/js") ||
        path.StartsWith("/_content"))
    {
        await next();
        return;
    }

    if (!isAuthenticated)
    {
        context.Response.Redirect("/login");
        return;
    }

    await next();
});

app.MapPost("/logout", async (SignInManager<ApplicationUser> signInManager, HttpContext context) =>
{
    await signInManager.SignOutAsync();
    
    var html = @"
<!DOCTYPE html>
<html>
<head><title>Logging out...</title></head>
<body>
<script>
    localStorage.clear();
    sessionStorage.clear();
    window.location.href = '/login';
</script>
</body>
</html>";
    
    context.Response.ContentType = "text/html";
    await context.Response.WriteAsync(html);
});

app.MapGet("/logout", async (SignInManager<ApplicationUser> signInManager, HttpContext context) =>
{
    await signInManager.SignOutAsync();
    
    var html = @"
<!DOCTYPE html>
<html>
<head><title>Logging out...</title></head>
<body>
<script>
    localStorage.clear();
    sessionStorage.clear();
    window.location.href = '/login';
</script>
</body>
</html>";
    
    context.Response.ContentType = "text/html";
    await context.Response.WriteAsync(html);
});

app.MapPost("/Account/Login", async (
    HttpContext context,
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager) => 
{
    try 
    {
        var form = await context.Request.ReadFormAsync();
        var username = form["username"].ToString();
        var password = form["password"].ToString();
        var rememberMe = form["rememberMe"].ToString().ToLower() == "true";
        var returnUrl = form["returnUrl"].ToString();

        if (string.IsNullOrEmpty(returnUrl))
            returnUrl = "/";

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            return Results.Redirect("/login?error=missing");
        }

        // Check if user exists and is active BEFORE attempting sign-in
        var user = await userManager.FindByNameAsync(username);
        if (user == null)
        {
            return Results.Redirect("/login?error=failed");
        }

        // Check if user is active
        if (!user.IsActive)
        {
            return Results.Redirect("/login?error=inactive");
        }

        var result = await signInManager.PasswordSignInAsync(username, password, rememberMe, lockoutOnFailure: false);
        
        if (result.Succeeded)
        {
            return Results.Redirect(returnUrl);
        }
        else
        {
            return Results.Redirect("/login?error=failed");
        }
    }
    catch (Exception)
    {
        return Results.Redirect("/login?error=exception");
    }
});

// Endpoint mapping
app.MapBlazorHub();
app.MapRazorPages();
app.MapFallbackToPage("/_Host");

app.Run();
