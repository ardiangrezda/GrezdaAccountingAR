using Microsoft.EntityFrameworkCore;
using Accounting.Data;
using Accounting.Models;

namespace Accounting.Services
{
    public class SalesCategoryService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILocalizationService _localizationService;

        public SalesCategoryService(ApplicationDbContext context, ILocalizationService localizationService)
        {
            _context = context;
            _localizationService = localizationService;
        }

        public async Task<List<SalesCategory>> GetSalesCategoriesAsync()
        {
            return await _context.SalesCategories
                .Include(sc => sc.NameString)
                .Include(sc => sc.DescriptionString)
                .Where(sc => sc.IsActive)
                .OrderBy(sc => sc.Id)
                .ToListAsync();
        }

        public async Task<SalesCategory?> GetSalesCategoryByIdAsync(int id)
        {
            return await _context.SalesCategories
                .Include(sc => sc.NameString)
                .Include(sc => sc.DescriptionString)
                .FirstOrDefaultAsync(sc => sc.Id == id);
        }

        public async Task<SalesCategory?> GetSalesCategoryByCodeAsync(string code)
        {
            return await _context.SalesCategories
                .Include(sc => sc.NameString)
                .Include(sc => sc.DescriptionString)
                .FirstOrDefaultAsync(sc => sc.Code == code && sc.IsActive);
        }
    }
}
