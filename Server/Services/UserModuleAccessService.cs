using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;

namespace Server.Services
{
    public class UserModuleAccessService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public UserModuleAccessService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task<List<ApplicationUser>> GetUsersAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return await db.Users.ToListAsync();
        }

        public async Task<List<Module>> GetModulesAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return await db.Modules.Include(m => m.Submodules).ToListAsync();
        }

        public async Task SaveUserAccessAsync(string userId, UserAccessViewModel access)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Remove existing access
            var existingAccess = db.UserModuleAccesses.Where(uma => uma.UserId == userId);
            db.UserModuleAccesses.RemoveRange(existingAccess);

            // Add new access
            foreach (var module in access.Modules.Where(m => m.Value))
            {
                db.UserModuleAccesses.Add(new UserModuleAccess
                {
                    UserId = userId,
                    ModuleId = module.Key
                });
            }

            foreach (var submodule in access.Submodules.Where(s => s.Value))
            {
                var submoduleEntity = await db.Submodules.FindAsync(submodule.Key);
                if (submoduleEntity != null)
                {
                    db.UserModuleAccesses.Add(new UserModuleAccess
                    {
                        UserId = userId,
                        ModuleId = submoduleEntity.ModuleId,
                        SubmoduleId = submodule.Key
                    });
                }
            }

            await db.SaveChangesAsync();
        }

        public async Task<List<Module>> GetAllowedModulesAsync(string userId)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return await db.UserModuleAccesses
                .Where(uma => uma.UserId == userId && uma.SubmoduleId == null)
                .Select(uma => uma.Module!)
                .Distinct()
                .ToListAsync();
        }

        public async Task<List<Submodule>> GetAllowedSubmodulesAsync(string userId, int moduleId)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return await db.UserModuleAccesses
                .Where(uma => uma.UserId == userId && uma.ModuleId == moduleId && uma.SubmoduleId != null)
                .Select(uma => uma.Submodule!)
                .Distinct()
                .ToListAsync();
        }

        // Check whether a user has access to a RazorPage, optionally constrained by variantCode.
        public async Task<bool> HasAccessToPageAsync(string userId, string razorPage, string? variantCode = null)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(razorPage))
                return false;

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // 1) Submodule-level access
            var hasSubmoduleAccess = await db.UserModuleAccesses
                .Where(uma => uma.UserId == userId && uma.SubmoduleId != null)
                .Select(uma => uma.Submodule!)
                .Where(s => s.RazorPage == razorPage && (s.VariantCode == null || s.VariantCode == variantCode))
                .AnyAsync();

            if (hasSubmoduleAccess)
                return true;

            // 2) Module-level access
            var hasModuleAccess = await db.UserModuleAccesses
                .Where(uma => uma.UserId == userId && uma.SubmoduleId == null)
                .Select(uma => uma.Module!)
                .Where(m => m.RazorPage == razorPage)
                .AnyAsync();

            return hasModuleAccess;
        }
    }

    public class UserAccessViewModel
    {
        public Dictionary<int, bool> Modules { get; set; } = new();
        public Dictionary<int, bool> Submodules { get; set; } = new();
    }
}