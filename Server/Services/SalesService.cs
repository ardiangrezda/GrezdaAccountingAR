using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;

namespace Server.Services
{
    public class SalesService
    {
        private readonly ApplicationDbContext _context;
        private readonly BusinessUnitStateContainer _stateContainer;

        public SalesService(ApplicationDbContext context, BusinessUnitStateContainer stateContainer)
        {
            _context = context;
            _stateContainer = stateContainer;
        }

        // Get all sales with optional filtering - Updated to include user filtering
        public async Task<List<SalesInvoice>> GetAllSalesAsync(int? businessUnitId = null, string? userId = null, bool includePosted = false, int? categoryId = null)
        {
            // Use the provided businessUnitId parameter, fallback to _stateContainer if not provided
            var effectiveBusinessUnitId = businessUnitId ?? _stateContainer.CurrentBusinessUnitId;
            
            var query = _context.SalesInvoices.AsQueryable();

            // Filter by business unit if one is selected
            if (effectiveBusinessUnitId.HasValue)
            {
                query = query.Where(s => s.BusinessUnitId == effectiveBusinessUnitId.Value);
            }

            // Filter by user ID if provided - NEW FILTER
            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(s => s.CreatedByUserId == userId);
            }

            if (!includePosted)
                query = query.Where(s => !s.IsPosted);

            if (categoryId.HasValue)
                query = query.Where(s => s.SalesCategoryId == categoryId.Value);

            return await query
                .Include(s => s.Buyer)
                .Include(s => s.Items)
                    .ThenInclude(item => item.Article)
                .Include(s => s.Items)
                    .ThenInclude(item => item.Unit)
                .Include(s => s.Items)
                    .ThenInclude(item => item.Currency)
                .Include(s => s.CreatedByUser)
                .OrderByDescending(s => s.InvoiceDate)
                .ToListAsync();
        }

        // Get a specific sale by ID - Updated to include user ownership check
        public async Task<SalesInvoice?> GetSaleByIdAsync(int id, string? userId = null)
        {
            var query = _context.SalesInvoices.AsQueryable();

            // Filter by user ID if provided
            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(s => s.CreatedByUserId == userId);
            }

            return await query
                .Include(s => s.Buyer)
                .Include(s => s.Items)
                    .ThenInclude(item => item.Article)
                .Include(s => s.Items)
                    .ThenInclude(item => item.Unit)
                .Include(s => s.Items)
                    .ThenInclude(item => item.Currency)
                .Include(s => s.CreatedByUser)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        // Create a new sale
        public async Task<SalesInvoice> CreateSaleAsync(SalesInvoice sale)
        {
            // Use the BusinessUnitId from the sale object, not from _stateContainer
            var businessUnitId = sale.BusinessUnitId;
            
            // Validate that BusinessUnitId is set
            if (businessUnitId <= 0)
            {
                throw new InvalidOperationException("Business unit ID is required in the sale object");
            }

            // Validate required fields
            if (sale.BuyerId <= 0)
            {
                throw new InvalidOperationException("Buyer ID is required");
            }

            // Find the buyer
            var buyer = await _context.Subjects
                .FirstOrDefaultAsync(s => s.Id == sale.BuyerId && s.IsBuyer)
                ?? throw new InvalidOperationException("Valid buyer not found");

            // The BusinessUnitId is already set in the sale object, so we don't need to set it again
            // sale.BusinessUnitId = businessUnitId; // This line is unnecessary now
            
            sale.BuyerCode = buyer.Code;
            sale.BuyerName = buyer.SubjectName;
            sale.CreatedAt = DateTime.UtcNow;

            if (sale.SalesCategoryId <= 0)
            {
                var domesticCategory = await _context.SalesCategories
                    .FirstOrDefaultAsync(sc => sc.Code == "DOM");
                
                sale.SalesCategoryId = domesticCategory?.Id ?? 1;
            }

            // Get the next sequential number for this business unit and sales category
            var lastInvoice = await _context.SalesInvoices
                .Where(s => s.BusinessUnitId == businessUnitId 
                        && s.SalesCategoryId == sale.SalesCategoryId)
                .OrderByDescending(s => s.SequentialNumber)
                .FirstOrDefaultAsync();

            sale.SequentialNumber = (lastInvoice?.SequentialNumber ?? 0) + 1;

            // Calculate totals
            sale.TotalWithoutVAT = sale.Items.Sum(item => item.ValueWithoutVAT);
            sale.TotalVATAmount = sale.Items.Sum(item => item.VATAmount);
            sale.TotalWithVAT = sale.Items.Sum(item => item.ValueWithVAT);
            sale.TotalDiscountAmount = sale.Items.Sum(item => item.DiscountAmount);

            _context.SalesInvoices.Add(sale);
            await _context.SaveChangesAsync();

            return sale;
        }

        // Update an existing sale - Updated to include user ownership check
        public async Task<bool> UpdateSaleAsync(SalesInvoice sale, string? userId = null)
        {
            if (sale.IsPosted)
            {
                throw new InvalidOperationException("Posted sales cannot be modified");
            }

            var query = _context.SalesInvoices.Include(s => s.Items).AsQueryable();

            // Filter by user ID if provided
            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(s => s.CreatedByUserId == userId);
            }

            var existingSale = await query.FirstOrDefaultAsync(s => s.Id == sale.Id);

            if (existingSale == null)
            {
                return false;
            }

            // Update the last modified information
            sale.LastModifiedAt = DateTime.UtcNow;
            sale.LastModifiedByUserId = userId;

            // Update totals
            sale.TotalWithoutVAT = sale.Items.Sum(item => item.ValueWithoutVAT);
            sale.TotalVATAmount = sale.Items.Sum(item => item.VATAmount);
            sale.TotalWithVAT = sale.Items.Sum(item => item.ValueWithVAT);
            sale.TotalDiscountAmount = sale.Items.Sum(item => item.DiscountAmount);

            // Handle invoice items
            // Remove items that are no longer present
            var existingItemIds = existingSale.Items.Select(i => i.Id).ToList();
            var updatedItemIds = sale.Items.Where(i => i.Id > 0).Select(i => i.Id).ToList();
            var itemsToRemove = existingSale.Items.Where(i => !updatedItemIds.Contains(i.Id)).ToList();
            
            foreach (var itemToRemove in itemsToRemove)
            {
                _context.SalesInvoiceItems.Remove(itemToRemove);
            }

            // Update existing items and add new ones
            foreach (var item in sale.Items)
            {
                if (item.Id > 0)
                {
                    // Update existing item
                    var existingItem = existingSale.Items.FirstOrDefault(i => i.Id == item.Id);
                    if (existingItem != null)
                    {
                        _context.Entry(existingItem).CurrentValues.SetValues(item);
                    }
                }
                else
                {
                    // Add new item
                    item.SalesInvoiceId = sale.Id;
                    existingSale.Items.Add(item);
                }
            }

            // Update the main invoice properties
            _context.Entry(existingSale).CurrentValues.SetValues(sale);

            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error updating invoice: {ex.Message}");
                return false;
            }
        }

        // Post a sale - Updated to include user ownership check
        public async Task<bool> PostSaleAsync(int id, string userId)
        {
            var sale = await _context.SalesInvoices
                .FirstOrDefaultAsync(s => s.Id == id && s.CreatedByUserId == userId);
            
            if (sale == null || sale.IsPosted)
            {
                return false;
            }

            sale.IsPosted = true;
            sale.PostedDate = DateTime.UtcNow;
            sale.LastModifiedByUserId = userId;
            sale.LastModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        // Cancel a sale - Updated to include user ownership check
        public async Task<bool> CancelSaleAsync(int id, string reason, string userId)
        {
            var sale = await _context.SalesInvoices
                .FirstOrDefaultAsync(s => s.Id == id && s.CreatedByUserId == userId);
            
            if (sale == null)
            {
                return false;
            }

            sale.IsCancelled = true;
            sale.CancellationReason = reason;
            sale.LastModifiedByUserId = userId;
            sale.LastModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        // Search sales - Updated to include user and business unit filtering
        public async Task<List<SalesInvoice>> SearchSalesAsync(string searchTerm, int? businessUnitId = null, string? userId = null)
        {
            var query = _context.SalesInvoices.AsQueryable();

            // Filter by business unit
            if (businessUnitId.HasValue)
            {
                query = query.Where(s => s.BusinessUnitId == businessUnitId.Value);
            }

            // Filter by user ID
            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(s => s.CreatedByUserId == userId);
            }

            return await query
                .Where(s => s.InvoiceNumber.Contains(searchTerm) ||
                           s.BuyerName.Contains(searchTerm))
                .Include(s => s.Buyer)
                .Include(s => s.Items)
                    .ThenInclude(item => item.Article)
                .OrderByDescending(s => s.InvoiceDate)
                .ToListAsync();
        }
    }
}