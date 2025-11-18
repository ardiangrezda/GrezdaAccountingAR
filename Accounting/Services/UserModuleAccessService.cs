using Microsoft.EntityFrameworkCore;
using Accounting.Data;
using Accounting.Models;

namespace Accounting.Services
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

            // Use a transaction to make remove+insert atomic and add diagnostics
            using var transaction = await db.Database.BeginTransactionAsync();
            try
            {
                // Load existing access rows for the user and remove them
                var existingAccess = await db.UserModuleAccesses.Where(uma => uma.UserId == userId).ToListAsync();
                if (existingAccess.Any())
                {
                    db.UserModuleAccesses.RemoveRange(existingAccess);
                }

                var toAdd = new List<UserModuleAccess>();

                // Add module-level access entries (SubmoduleId = null)
                foreach (var module in access.Modules.Where(m => m.Value))
                {
                    toAdd.Add(new UserModuleAccess
                    {
                        UserId = userId,
                        ModuleId = module.Key,
                        SubmoduleId = null
                    });
                }

                // Add submodule-level access entries (preserve module association)
                foreach (var submodule in access.Submodules.Where(s => s.Value))
                {
                    // ensure submodule exists and get its ModuleId
                    var submoduleEntity = await db.Submodules.SingleOrDefaultAsync(s => s.Id == submodule.Key);
                    if (submoduleEntity != null)
                    {
                        toAdd.Add(new UserModuleAccess
                        {
                            UserId = userId,
                            ModuleId = submoduleEntity.ModuleId,
                            SubmoduleId = submoduleEntity.Id
                        });
                    }
                }

                if (toAdd.Any())
                {
                    await db.UserModuleAccesses.AddRangeAsync(toAdd);
                }

                await db.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                try { await transaction.RollbackAsync(); } catch { /* ignore rollback errors */ }
                throw;
            }
        }

        public async Task<List<Module>> GetAllowedModulesAsync(string userId)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var totalForUser = await db.UserModuleAccesses.CountAsync(uma => uma.UserId == userId);
            var moduleLevelCount = await db.UserModuleAccesses.CountAsync(uma => uma.UserId == userId && uma.SubmoduleId == null);

            // Explicit join to Modules table
            var query = from uma in db.UserModuleAccesses
                        where uma.UserId == userId && uma.SubmoduleId == null
                        join m in db.Modules on uma.ModuleId equals m.Id
                        select m;

            return await query.Distinct().ToListAsync();
        }

        public async Task<List<Submodule>> GetAllowedSubmodulesAsync(string userId, int moduleId)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var submoduleRows = await db.UserModuleAccesses
                .Where(uma => uma.UserId == userId && uma.ModuleId == moduleId && uma.SubmoduleId != null)
                .Select(uma => uma.SubmoduleId)
                .ToListAsync();


            var query = from uma in db.UserModuleAccesses
                        where uma.UserId == userId && uma.ModuleId == moduleId && uma.SubmoduleId != null
                        join s in db.Submodules on uma.SubmoduleId equals s.Id
                        select s;

            return await query.Distinct().ToListAsync();
        }

        // Check whether a user has access to a RazorPage, optionally constrained by variantCode.
        public async Task<bool> HasAccessToPageAsync(string userId, string razorPage, string? variantCode = null)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(razorPage))
                return false;

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // 1) Submodule-level access (explicit join)
            var subQuery = from uma in db.UserModuleAccesses
                           join s in db.Submodules on uma.SubmoduleId equals s.Id
                           where uma.UserId == userId
                                 && uma.SubmoduleId != null
                                 && s.RazorPage != null
                                 && (s.VariantCode == null || s.VariantCode == variantCode)
                           select uma.Id;

            if (await subQuery.AnyAsync())
                return true;

            // 2) Module-level access (explicit join)
            var modQuery = from uma in db.UserModuleAccesses
                           join m in db.Modules on uma.ModuleId equals m.Id
                           where uma.UserId == userId
                                 && uma.SubmoduleId == null
                                 && m.RazorPage != null
                           select uma.Id;

            return await modQuery.AnyAsync();
        }

        // NEW: load roles for a set of users using the Identity tables directly
        public async Task<Dictionary<string, string>> GetRolesForUsersAsync(IEnumerable<string> userIds)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var ids = userIds?.Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList() ?? new List<string>();

            if (!ids.Any())
                return new Dictionary<string, string>();

            var userRolePairs = await db.UserRoles
                .Where(ur => ids.Contains(ur.UserId))
                .Join(db.Roles,
                      ur => ur.RoleId,
                      r => r.Id,
                      (ur, r) => new { ur.UserId, RoleName = r.Name })
                .ToListAsync();

            var dict = userRolePairs
                .GroupBy(x => x.UserId)
                .ToDictionary(g => g.Key, g => string.Join(", ", g.Select(x => x.RoleName)));

            // ensure every requested userId has an entry (empty string if no role)
            foreach (var id in ids)
            {
                if (!dict.ContainsKey(id))
                    dict[id] = string.Empty;
            }

            return dict;
        }

        // Diagnostic helper (already suggested previously) - useful to verify what's persisted.
        public async Task<List<UserModuleAccess>> GetUserAccessEntriesAsync(string userId)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            return await db.UserModuleAccesses
                .Where(uma => uma.UserId == userId)
                .Include(uma => uma.Submodule)
                .Include(uma => uma.Module)
                .ToListAsync();
        }
    }

    public class UserAccessViewModel
    {
        public Dictionary<int, bool> Modules { get; set; } = new();
        public Dictionary<int, bool> Submodules { get; set; } = new();
    }
}