using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;

namespace Server.Services
{
    public class UserModuleAccessService
    {
        private readonly ApplicationDbContext _dbContext;

        public UserModuleAccessService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<ApplicationUser>> GetUsersAsync()
        {
            return await _dbContext.Users.ToListAsync();
        }

        public async Task<List<Module>> GetModulesAsync()
        {
            return await _dbContext.Modules.Include(m => m.Submodules).ToListAsync();
        }

        public async Task SaveUserAccessAsync(string userId, UserAccessViewModel access)
        {
            // Remove existing access
            var existingAccess = _dbContext.UserModuleAccesses.Where(uma => uma.UserId == userId);
            _dbContext.UserModuleAccesses.RemoveRange(existingAccess);

            // Add new access
            foreach (var module in access.Modules.Where(m => m.Value))
            {
                _dbContext.UserModuleAccesses.Add(new UserModuleAccess
                {
                    UserId = userId,
                    ModuleId = module.Key
                });
            }

            foreach (var submodule in access.Submodules.Where(s => s.Value))
            {
                var submoduleEntity = await _dbContext.Submodules.FindAsync(submodule.Key);
                if (submoduleEntity != null)
                {
                    _dbContext.UserModuleAccesses.Add(new UserModuleAccess
                    {
                        UserId = userId,
                        ModuleId = submoduleEntity.ModuleId,
                        SubmoduleId = submodule.Key
                    });
                }
            }

            await _dbContext.SaveChangesAsync();
        }
        public async Task<List<Module>> GetAllowedModulesAsync(string userId)
        {
            return await _dbContext.UserModuleAccesses
                .Where(uma => uma.UserId == userId && uma.SubmoduleId == null)
                .Select(uma => uma.Module!)
                .Distinct()
                .ToListAsync();
        }

        public async Task<List<Submodule>> GetAllowedSubmodulesAsync(string userId, int moduleId)
        {
            return await _dbContext.UserModuleAccesses
                .Where(uma => uma.UserId == userId && uma.ModuleId == moduleId && uma.SubmoduleId != null)
                .Select(uma => uma.Submodule!)
                .Distinct()
                .ToListAsync();
        }
    }

    public class UserAccessViewModel
    {
        public Dictionary<int, bool> Modules { get; set; } = new();
        public Dictionary<int, bool> Submodules { get; set; } = new();
    }
}