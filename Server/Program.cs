using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;
using Server.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    // Password settings
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    
    // User settings
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false;
    
    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
})
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
builder.Services.AddServerSideBlazor(options =>
{
    options.DetailedErrors = true;
})
.AddHubOptions(options =>
{
    options.MaximumReceiveMessageSize = 32 * 1024 * 1024; // 32MB
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
    options.HandshakeTimeout = TimeSpan.FromSeconds(15);
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
    SignInManager<ApplicationUser> signInManager) =>
{
    try 
    {
        var form = await context.Request.ReadFormAsync();
        var email = form["email"].ToString();
        var password = form["password"].ToString();
        var rememberMe = form["rememberMe"].ToString().ToLower() == "true";
        var returnUrl = form["returnUrl"].ToString();

        if (string.IsNullOrEmpty(returnUrl))
            returnUrl = "/";

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            return Results.Redirect("/login?error=missing");
        }

        var result = await signInManager.PasswordSignInAsync(email, password, rememberMe, lockoutOnFailure: false);
        
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
