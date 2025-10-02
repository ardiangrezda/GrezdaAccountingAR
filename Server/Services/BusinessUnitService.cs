using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;

namespace Server.Services
{
    public class BusinessUnitService
    {
        private readonly ApplicationDbContext _context;

        public BusinessUnitService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<BusinessUnit>> GetAllAsync()
        {
            return await _context.BusinessUnits.OrderBy(u => u.Code).ToListAsync();
        }

        public async Task<BusinessUnit?> GetByIdAsync(int id)
        {
            return await _context.BusinessUnits.FindAsync(id);
        }

        public async Task<BusinessUnit> CreateAsync(BusinessUnit businessUnit)
        {
            _context.BusinessUnits.Add(businessUnit);
            await _context.SaveChangesAsync();
            return businessUnit;
        }

        public async Task UpdateAsync(BusinessUnit businessUnit)
        {
            businessUnit.LastModifiedAt = DateTime.UtcNow;
            _context.BusinessUnits.Update(businessUnit);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var businessUnit = await _context.BusinessUnits.FindAsync(id);
            if (businessUnit != null)
            {
                _context.BusinessUnits.Remove(businessUnit);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<BusinessUnit>> GetBusinessUnitsForUserAsync(string userId)
        {
            return await _context.UserBusinessUnits
                .Where(ub => ub.UserId == userId && ub.IsActive)
                .Select(ub => ub.BusinessUnit)
                .ToListAsync();
        }

        public async Task<List<UserBusinessUnit>> GetUserBusinessUnitsAsync(string userId)
        {
            return await _context.UserBusinessUnits
                .Where(ub => ub.UserId == userId)
                .ToListAsync();
        }

        public async Task AssignUserToBusinessUnitAsync(string userId, int businessUnitId)
        {
            var existing = await _context.UserBusinessUnits
                .FirstOrDefaultAsync(ub => ub.UserId == userId && ub.BusinessUnitId == businessUnitId);

            if (existing == null)
            {
                _context.UserBusinessUnits.Add(new UserBusinessUnit
                {
                    UserId = userId,
                    BusinessUnitId = businessUnitId,
                    AssignedAt = DateTime.UtcNow,
                    IsActive = true
                });

                await _context.SaveChangesAsync();
            }
        }

        public async Task RemoveUserFromBusinessUnitAsync(string userId, int businessUnitId)
        {
            var assignment = await _context.UserBusinessUnits
                .FirstOrDefaultAsync(ub => ub.UserId == userId && ub.BusinessUnitId == businessUnitId);

            if (assignment != null)
            {
                _context.UserBusinessUnits.Remove(assignment);
                await _context.SaveChangesAsync();
            }
        }
    }
}

