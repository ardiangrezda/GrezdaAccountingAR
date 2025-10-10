using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;

namespace Server.Services
{
    public class ArticleService
    {
        private readonly ApplicationDbContext _context;

        public ArticleService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Article>> GetAllArticlesAsync(bool includeInactive = false)
        {
            var query = _context.Articles
                .Include(a => a.Unit)
                .Include(a => a.Currency)
                .Include(a => a.VATTable)
                .AsQueryable();

            if (!includeInactive)
            {
                query = query.Where(a => a.IsActive);
            }

            return await query
                .OrderBy(a => a.Code)
                .ToListAsync();
        }

        public async Task<Article?> GetArticleByIdAsync(int id)
        {
            return await _context.Articles
                .Include(a => a.Unit)
                .Include(a => a.Currency)
                .Include(a => a.VATTable)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<Article?> GetArticleByBarcodeAsync(string barcode)
        {
            return await _context.Articles
                .Include(a => a.Unit)
                .Include(a => a.Currency)
                .Include(a => a.VATTable)
                .FirstOrDefaultAsync(a => a.Barcode == barcode && a.IsActive);
        }

        public async Task<List<Article>> SearchArticlesAsync(string searchTerm, bool includeInactive = false)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllArticlesAsync(includeInactive);

            var query = _context.Articles
                .Include(a => a.Unit)
                .Include(a => a.Currency)
                .Include(a => a.VATTable) 
                .AsQueryable();

            if (!includeInactive)
            {
                query = query.Where(a => a.IsActive);
            }

            return await query
                .Where(a => a.Code.Contains(searchTerm) ||
                           (a.Barcode != null && a.Barcode.Contains(searchTerm)) || 
                           a.Description.Contains(searchTerm) ||
                           a.Category.Contains(searchTerm))
                .OrderBy(a => a.Code)
                .ToListAsync();
        }

        public async Task<bool> UpdateArticleAsync(Article article)
        {
            var existingArticle = await _context.Articles.FindAsync(article.Id);
            if (existingArticle == null) return false;

            existingArticle.Code = article.Code;
            existingArticle.Barcode = article.Barcode;
            existingArticle.Description = article.Description;
            existingArticle.Description2 = article.Description2;
            existingArticle.Description3 = article.Description3;
            existingArticle.UnitId = article.UnitId;
            existingArticle.Price = article.Price;
            existingArticle.CurrencyId = article.CurrencyId;
            existingArticle.VATId = article.VATId;
            existingArticle.StockQuantity = article.StockQuantity;
            existingArticle.Category = article.Category;
            existingArticle.IsActive = article.IsActive;
            existingArticle.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CheckBarcodeExistsAsync(string barcode, int? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(barcode)) return false;

            var query = _context.Articles.Where(a => a.Barcode == barcode);

            if (excludeId.HasValue)
            {
                query = query.Where(a => a.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        public async Task<List<Article>> GetArticlesByCategoryAsync(string category, bool includeInactive = false)
        {
            var query = _context.Articles
                .Where(a => a.Category == category);

            if (!includeInactive)
            {
                query = query.Where(a => a.IsActive);
            }

            return await query
                .OrderBy(a => a.Code)
                .ToListAsync();
        }

        public async Task<List<VATTable>> GetAllVATTypesAsync()
        {
            return await _context.VATTable
                .Where(v => v.IsActive)
                .OrderBy(v => v.VATRate)
                .ToListAsync();
        }

        public async Task<List<string>> GetCategoriesAsync()
        {
            return await _context.Articles
                .Where(a => a.IsActive && !string.IsNullOrEmpty(a.Category))
                .Select(a => a.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
        }

        public async Task<Article> CreateArticleAsync(Article article)
        {
            article.CreatedAt = DateTime.UtcNow;
            article.UpdatedAt = null;

            _context.Articles.Add(article);
            await _context.SaveChangesAsync();
            return article;
        }

        public async Task<bool> DeleteArticleAsync(int id)
        {
            var article = await _context.Articles.FindAsync(id);
            if (article == null) return false;

            article.IsActive = false;
            article.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RestoreArticleAsync(int id)
        {
            var article = await _context.Articles.FindAsync(id);
            if (article == null) return false;

            article.IsActive = true;
            article.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CheckCodeExistsAsync(string code, int? excludeId = null)
        {
            var query = _context.Articles.Where(a => a.Code == code);

            if (excludeId.HasValue)
            {
                query = query.Where(a => a.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        public async Task<List<Article>> GetLowStockArticlesAsync(int threshold = 10)
        {
            return await _context.Articles
                .Where(a => a.IsActive && a.StockQuantity <= threshold)
                .OrderBy(a => a.StockQuantity)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalInventoryValueAsync()
        {
            return await _context.Articles
                .Where(a => a.IsActive)
                .SumAsync(a => a.Price * a.StockQuantity);
        }

        public async Task<List<Unit>> GetAllUnitsAsync()
        {
            return await _context.Units
                .OrderBy(u => u.Description)
                .ToListAsync();
        }

        public async Task<List<Currency>> GetAllCurrenciesAsync()
        {
            return await _context.Currencies
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<bool> UpdateStockQuantitiesAsync(List<(int articleId, decimal quantityToSubtract)> stockUpdates)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var (articleId, quantityToSubtract) in stockUpdates)
                {
                    var article = await _context.Articles.FindAsync(articleId);
                    if (article == null)
                    {
                        throw new InvalidOperationException($"Article with ID {articleId} not found");
                    }

                    // Subtract the quantity (can go negative)
                    article.StockQuantity -= (int)quantityToSubtract;
                    article.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<Article>> GetNegativeStockArticlesAsync()
        {
            return await _context.Articles
                .Include(a => a.Unit)
                .Include(a => a.Currency)
                .Where(a => a.IsActive && a.StockQuantity < 0)
                .OrderBy(a => a.StockQuantity)
                .ToListAsync();
        }
    }
}