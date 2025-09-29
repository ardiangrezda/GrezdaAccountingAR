using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;
using Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity and Authentication configuration
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

// Cookie configuration
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

// Your services
builder.Services.AddScoped<ArticleService>();
builder.Services.AddScoped<ILocalizationService, LocalizationService>();
builder.Services.AddScoped<SubjectService>();
builder.Services.AddScoped<SalesService>();
builder.Services.AddScoped<SalesCategoryService>();
builder.Services.AddSingleton<StateContainer>();

// CORS configuration
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

// Create users and roles
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    // Temporarily remove existing users (remove this after users are properly set up)
    var existingUsers = new[] { "ardi1@accounting.com", "ardi22@accounting.com", "ardi1111@accounting.com" };
    foreach (var email in existingUsers)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user != null)
        {
            await userManager.DeleteAsync(user);
        }
    }

    // Create roles if they don't exist
    var roles = new[] { "Admin", "Accountant", "User" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    // Create admin user
    var adminEmail = "admin@accounting.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FirstName = "Admin",
            LastName = "User",
            EmailConfirmed = true
        };
        await userManager.CreateAsync(adminUser, "Admin123!");
        await userManager.AddToRoleAsync(adminUser, "Admin");
    }

    // Create additional users
    var users = new[]
    {
        new { Username = "ardi1", Email = "ardi1@accounting.com", Role = "Accountant", Password = "Ardi1!123" },
        new { Username = "ardi22", Email = "ardi22@accounting.com", Role = "User", Password = "123456" },
        new { Username = "ardi1111", Email = "ardi1111@accounting.com", Role = "Admin", Password = "123456" }
    };

    foreach (var userData in users)
    {
        var user = await userManager.FindByEmailAsync(userData.Email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = userData.Email, // Changed to use email as username
                Email = userData.Email,
                FirstName = userData.Username,
                LastName = "User",
                EmailConfirmed = true,
                IsActive = true
            };

            var result = await userManager.CreateAsync(user, userData.Password);
            if (result.Succeeded)
            {
                var roleResult = await userManager.AddToRoleAsync(user, userData.Role);
                Console.WriteLine($"User {userData.Email} created successfully");
                Console.WriteLine($"Role assignment {(roleResult.Succeeded ? "succeeded" : "failed")}");
                
                // Verify the user can be found and password works
                var createdUser = await userManager.FindByEmailAsync(userData.Email);
                if (createdUser != null)
                {
                    var passwordValid = await userManager.CheckPasswordAsync(createdUser, userData.Password);
                    Console.WriteLine($"Password verification: {passwordValid}");
                }
            }
            else
            {
                Console.WriteLine($"Failed to create user {userData.Email}:");
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"- {error.Description}");
                }
            }
        }
    }
}

// Configure pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

// Error handling middleware
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Unhandled exception: {ex}");
        throw;
    }
});

app.UseEndpoints(endpoints =>
{
    endpoints.MapBlazorHub();
    endpoints.MapFallbackToPage("/_Host");
    endpoints.MapRazorPages();
});

// Your authentication check middleware
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/" && !context.User.Identity.IsAuthenticated)
    {
        context.Response.Redirect("/login");
        return;
    }
    await next();
});

app.Run();