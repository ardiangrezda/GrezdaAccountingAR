using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;

namespace Server.Services
{
    public class BusinessUnitService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

        public BusinessUnitService(IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<List<BusinessUnit>> GetAllAsync()
        {
            using var context = _contextFactory.CreateDbContext();
            return await context.BusinessUnits
                .Where(bu => bu.IsActive)
                .OrderBy(bu => bu.Name)
                .ToListAsync();
        }

        public async Task<BusinessUnit?> GetByIdAsync(int id)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.BusinessUnits.FindAsync(id);
        }

        public async Task<BusinessUnit> CreateAsync(BusinessUnit businessUnit)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            context.BusinessUnits.Add(businessUnit);
            await context.SaveChangesAsync();
            return businessUnit;
        }

        public async Task UpdateAsync(BusinessUnit businessUnit)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            businessUnit.LastModifiedAt = DateTime.UtcNow;
            context.BusinessUnits.Update(businessUnit);
            await context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var businessUnit = await context.BusinessUnits.FindAsync(id);
            if (businessUnit != null)
            {
                context.BusinessUnits.Remove(businessUnit);
                await context.SaveChangesAsync();
            }
        }

        public async Task<List<BusinessUnit>> GetUserBusinessUnitsAsync(string userId, bool isAdmin)
        {
            using var context = _contextFactory.CreateDbContext();

            if (isAdmin)
            {
                // Admin can access all active business units
                return await context.BusinessUnits
                    .Where(bu => bu.IsActive)
                    .OrderBy(bu => bu.Name)
                    .ToListAsync();
            }
            else
            {
                // Regular user can only access assigned business units
                return await context.UserBusinessUnits
                    .Where(ub => ub.UserId == userId)
                    .Select(ub => ub.BusinessUnit)
                    .Where(bu => bu.IsActive)
                    .OrderBy(bu => bu.Name)
                    .ToListAsync();
            }
        }

        public async Task<BusinessUnit?> GetDefaultBusinessUnitAsync(string userId, bool isAdmin)
        {
            var units = await GetUserBusinessUnitsAsync(userId, isAdmin);
            return units.Count == 1 ? units.First() : null;
        }

        public async Task<List<BusinessUnit>> GetBusinessUnitsForUserAsync(string userId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.UserBusinessUnits
                .Where(ub => ub.UserId == userId && ub.IsActive)
                .Select(ub => ub.BusinessUnit)
                .ToListAsync();
        }

        public async Task<List<UserBusinessUnit>> GetUserBusinessUnitsAsync(string userId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.UserBusinessUnits
                .Where(ub => ub.UserId == userId)
                .ToListAsync();
        }

        public async Task AssignUserToBusinessUnitAsync(string userId, int businessUnitId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var existing = await context.UserBusinessUnits
                .FirstOrDefaultAsync(ub => ub.UserId == userId && ub.BusinessUnitId == businessUnitId);

            if (existing == null)
            {
                context.UserBusinessUnits.Add(new UserBusinessUnit
                {
                    UserId = userId,
                    BusinessUnitId = businessUnitId,
                    AssignedAt = DateTime.UtcNow,
                    IsActive = true
                });

                await context.SaveChangesAsync();
            }
        }

        public async Task RemoveUserFromBusinessUnitAsync(string userId, int businessUnitId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var assignment = await context.UserBusinessUnits
                .FirstOrDefaultAsync(ub => ub.UserId == userId && ub.BusinessUnitId == businessUnitId);

            if (assignment != null)
            {
                context.UserBusinessUnits.Remove(assignment);
                await context.SaveChangesAsync();
            }
        }
    }
}

