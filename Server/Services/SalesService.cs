using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;

namespace Server.Services
{
    public class SalesService
    {
        private readonly ApplicationDbContext _context;

        public SalesService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Get all sales with optional filtering
        public async Task<List<SalesInvoice>> GetAllSalesAsync(bool includePosted = false)
        {
            return await _context.SalesInvoices
                .Where(s => includePosted || !s.IsPosted)
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

        // Get a specific sale by ID
        public async Task<SalesInvoice?> GetSaleByIdAsync(int id)
        {
            return await _context.SalesInvoices
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
            // Validate required fields
            if (sale.BuyerId <= 0)
            {
                throw new InvalidOperationException("Buyer ID is required");
            }

            // Find the buyer
            var buyer = await _context.Subjects
                .FirstOrDefaultAsync(s => s.Id == sale.BuyerId && s.IsBuyer)
                ?? throw new InvalidOperationException("Valid buyer not found");

            sale.BuyerCode = buyer.Code;
            sale.BuyerName = buyer.SubjectName;
            sale.CreatedAt = DateTime.UtcNow;

            if (sale.SalesCategoryId <= 0)
            {
                // Default to Domestic Sales (DOM) only when not already set
                var domesticCategory = await _context.SalesCategories
                    .FirstOrDefaultAsync(sc => sc.Code == "DOM");
                
                if (domesticCategory != null)
                    sale.SalesCategoryId = domesticCategory.Id;
                else
                    sale.SalesCategoryId = 1;
            }

            // Calculate totals
            sale.TotalWithoutVAT = sale.Items.Sum(item => item.ValueWithoutVAT);
            sale.TotalVATAmount = sale.Items.Sum(item => item.VATAmount);
            sale.TotalWithVAT = sale.Items.Sum(item => item.ValueWithVAT);
            sale.TotalDiscountAmount = sale.Items.Sum(item => item.DiscountAmount);

            _context.SalesInvoices.Add(sale);
            await _context.SaveChangesAsync();

            return sale;
        }

        // Update an existing sale
        public async Task<bool> UpdateSaleAsync(SalesInvoice sale)
        {
            if (sale.IsPosted)
            {
                throw new InvalidOperationException("Posted sales cannot be modified");
            }

            var existingSale = await _context.SalesInvoices
                .Include(s => s.Items)
                .FirstOrDefaultAsync(s => s.Id == sale.Id);

            if (existingSale == null)
            {
                return false;
            }

            // Update the last modified information
            sale.LastModifiedAt = DateTime.UtcNow;

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

        // Post a sale
        public async Task<bool> PostSaleAsync(int id, string userId)
        {
            var sale = await _context.SalesInvoices.FindAsync(id);
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

        // Cancel a sale
        public async Task<bool> CancelSaleAsync(int id, string reason, string userId)
        {
            var sale = await _context.SalesInvoices.FindAsync(id);
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

        // Search sales
        public async Task<List<SalesInvoice>> SearchSalesAsync(string searchTerm)
        {
            return await _context.SalesInvoices
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