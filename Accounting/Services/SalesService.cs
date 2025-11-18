using Microsoft.EntityFrameworkCore;
using Accounting.Data;
using Accounting.Models;

namespace Accounting.Services
{
    public class SalesService
    {
        private readonly ApplicationDbContext _context;
        private readonly BusinessUnitStateContainer _stateContainer;
        private readonly ArticleService _articleService;

        public SalesService(ApplicationDbContext context, BusinessUnitStateContainer stateContainer, ArticleService articleService)
        {
            _context = context;
            _stateContainer = stateContainer;
            _articleService = articleService;
        }

        public async Task<List<SalesInvoice>> GetAllSalesAsync(int? businessUnitId = null, string? userId = null, bool includePosted = false, int? categoryId = null)
        {
            var effectiveBusinessUnitId = businessUnitId ?? _stateContainer.CurrentBusinessUnitId;
            
            var query = _context.SalesInvoices.AsQueryable();

            if (effectiveBusinessUnitId.HasValue)
            {
                query = query.Where(s => s.BusinessUnitId == effectiveBusinessUnitId.Value);
            }

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
                .Include(s => s.OriginalInvoice) // Include original invoice for returns
                .Include(s => s.Items)
                    .ThenInclude(item => item.Article)
                .Include(s => s.Items)
                    .ThenInclude(item => item.Unit)
                .Include(s => s.Items)
                    .ThenInclude(item => item.Currency)
                .Include(s => s.Items)
                    .ThenInclude(item => item.OriginalInvoiceItem) // Include original item for returns
                .Include(s => s.CreatedByUser)
                .OrderByDescending(s => s.InvoiceDate)
                .ToListAsync();
        }

        public async Task<SalesInvoice?> GetSaleByIdAsync(int id, string? userId = null)
        {
            var query = _context.SalesInvoices.AsQueryable();

            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(s => s.CreatedByUserId == userId);
            }

            return await query
                .Include(s => s.Buyer)
                .Include(s => s.OriginalInvoice) // Include original invoice for returns
                .Include(s => s.Items)
                    .ThenInclude(item => item.Article)
                .Include(s => s.Items)
                    .ThenInclude(item => item.Unit)
                .Include(s => s.Items)
                    .ThenInclude(item => item.Currency)
                .Include(s => s.Items)
                    .ThenInclude(item => item.OriginalInvoiceItem) // Include original item for returns
                .Include(s => s.CreatedByUser)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<List<SalesInvoice>> GetPostedInvoicesForReturnAsync(int businessUnitId, string? buyerName = null)
        {
            var query = _context.SalesInvoices
                .Where(s => s.BusinessUnitId == businessUnitId && 
                           s.IsPosted && 
                           !s.IsCancelled && 
                           !s.IsReturn) // Only original invoices, not returns
                .Include(s => s.Buyer)
                .Include(s => s.Items)
                    .ThenInclude(item => item.Article)
                .AsQueryable();

            if (!string.IsNullOrEmpty(buyerName))
            {
                query = query.Where(s => s.BuyerName.Contains(buyerName));
            }

            return await query
                .OrderByDescending(s => s.InvoiceDate)
                .Take(50) // Limit results for performance
                .ToListAsync();
        }

        public async Task<SalesInvoice?> GetOriginalInvoiceByNumberAsync(string invoiceNumber, int businessUnitId)
        {
            return await _context.SalesInvoices
                .Include(s => s.Buyer)
                .Include(s => s.Items)
                    .ThenInclude(item => item.Article)
                .Include(s => s.Items)
                    .ThenInclude(item => item.Unit)
                .FirstOrDefaultAsync(s => s.InvoiceNumber == invoiceNumber && 
                                        s.BusinessUnitId == businessUnitId && 
                                        s.IsPosted && 
                                        !s.IsCancelled && 
                                        !s.IsReturn);
        }

        public async Task<decimal> GetReturnableQuantityAsync(int originalItemId)
        {
            var originalItem = await _context.SalesInvoiceItems
                .FirstOrDefaultAsync(i => i.Id == originalItemId);

            if (originalItem == null) return 0;

            // Get total quantity already returned
            var totalReturned = await _context.SalesInvoiceItems
                .Where(i => i.OriginalInvoiceItemId == originalItemId)
                .Include(i => i.SalesInvoice)
                .Where(i => i.SalesInvoice.IsReturn && i.SalesInvoice.IsPosted)
                .SumAsync(i => i.Quantity);

            return originalItem.Quantity - totalReturned;
        }

        public async Task<(bool isValid, string errorMessage)> ValidateReturnQuantitiesAsync(List<SalesInvoiceItem> returnItems)
        {
            foreach (var returnItem in returnItems.Where(i => i.OriginalInvoiceItemId.HasValue))
            {
                var returnableQty = await GetReturnableQuantityAsync(returnItem.OriginalInvoiceItemId!.Value);
                
                if (returnItem.Quantity > returnableQty)
                {
                    return (false, $"Cannot return {returnItem.Quantity} of '{returnItem.Description}'. Only {returnableQty} available for return.");
                }
            }

            return (true, string.Empty);
        }

        private async Task<(string invoiceNumber, int sequentialNumber)> GenerateInvoiceNumberAsync(int businessUnitId, int salesCategoryId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var format = await _context.InvoiceNumberFormats
                    .Where(f => f.BusinessUnitId == businessUnitId && f.SalesCategoryId == salesCategoryId)
                    .FirstOrDefaultAsync();

                if (format == null)
                {
                    format = new InvoiceNumberFormat
                    {
                        BusinessUnitId = businessUnitId,
                        SalesCategoryId = salesCategoryId,
                        UseYear = true,
                        UseSalesCategoryCode = true,
                        UseBusinessUnitCode = true,
                        UseSequentialNumber = true,
                        Separator = "-",
                        SequentialNumberLength = 4,
                        LastUsedSequentialNumber = 0,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.InvoiceNumberFormats.Add(format);
                    await _context.SaveChangesAsync();
                }

                format.LastUsedSequentialNumber++;
                format.LastModifiedAt = DateTime.UtcNow;
                var nextSequentialNumber = format.LastUsedSequentialNumber;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var invoiceNumber = await FormatInvoiceNumber(format, nextSequentialNumber);
                
                return (invoiceNumber, nextSequentialNumber);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task<string> FormatInvoiceNumber(InvoiceNumberFormat format, int sequentialNumber)
        {
            var parts = new List<string>();

            if (format.UseYear)
            {
                var currentYear = DateTime.Now.Year;
                var yearPart = (currentYear % 100).ToString();
                parts.Add(yearPart);
            }

            if (format.UseSalesCategoryCode)
            {
                var salesCategory = await _context.SalesCategories
                    .FirstOrDefaultAsync(sc => sc.Id == format.SalesCategoryId);
                if (salesCategory != null && !string.IsNullOrEmpty(salesCategory.Code))
                {
                    parts.Add(salesCategory.Code);
                }
            }

            if (format.UseBusinessUnitCode)
            {
                var businessUnit = await _context.BusinessUnits
                    .FirstOrDefaultAsync(bu => bu.Id == format.BusinessUnitId);
                
                var businessUnitCode = businessUnit?.Code ?? format.BusinessUnitId.ToString("D3");
                parts.Add(businessUnitCode);
            }

            if (format.UseSequentialNumber)
            {
                var sequentialPart = sequentialNumber.ToString($"D{format.SequentialNumberLength}");
                parts.Add(sequentialPart);
            }

            return string.Join(format.Separator, parts);
        }

        public async Task<SalesInvoice> CreateSaleAsync(SalesInvoice sale)
        {
            var businessUnitId = sale.BusinessUnitId;
            
            if (businessUnitId <= 0)
            {
                throw new InvalidOperationException("Business unit ID is required in the sale object");
            }

            if (sale.BuyerId <= 0)
            {
                throw new InvalidOperationException("Buyer ID is required");
            }

            var buyer = await _context.Subjects
                .FirstOrDefaultAsync(s => s.Id == sale.BuyerId && s.IsBuyer)
                ?? throw new InvalidOperationException("Valid buyer not found");

            sale.BuyerCode = buyer.Code;
            sale.BuyerName = buyer.SubjectName;
            sale.CreatedAt = DateTime.UtcNow;

            if (sale.SalesCategoryId <= 0)
            {
                var domesticCategory = await _context.SalesCategories
                    .FirstOrDefaultAsync(sc => sc.Code == "DOM");
                
                sale.SalesCategoryId = domesticCategory?.Id ?? 1;
            }

            if (sale.IsReturn && sale.Items != null)
            {
                var validation = await ValidateReturnQuantitiesAsync(sale.Items.ToList());
                if (!validation.isValid)
                {
                    throw new InvalidOperationException(validation.errorMessage);
                }
            }

            // STEP 1: Generate invoice number and sequential number atomically
            var (invoiceNumber, sequentialNumber) = await GenerateInvoiceNumberAsync(businessUnitId, sale.SalesCategoryId);
            
            sale.InvoiceNumber = invoiceNumber;
            sale.SequentialNumber = sequentialNumber;

            // STEP 2: Calculate totals
            sale.TotalWithoutVAT = sale.Items?.Sum(item => item.ValueWithoutVAT) ?? 0;
            sale.TotalVATAmount = sale.Items?.Sum(item => item.VATAmount) ?? 0;
            sale.TotalWithVAT = sale.Items?.Sum(item => item.ValueWithVAT) ?? 0;
            sale.TotalDiscountAmount = sale.Items?.Sum(item => item.DiscountAmount) ?? 0;

            // STEP 3: Save the invoice to database
            _context.SalesInvoices.Add(sale);
            await _context.SaveChangesAsync();

            // STEP 4: Update stock quantities
            if (sale.Items != null && sale.Items.Any())
            {
                var stockUpdates = new List<(int articleId, decimal quantityChange)>();

                foreach (var item in sale.Items.Where(i => i.ArticleId > 0))
                {
                    if (sale.IsReturn)
                    {
                        // For returns: ADD quantity back to stock (negative change to subtract from UpdateStockQuantitiesAsync)
                        stockUpdates.Add((item.ArticleId, -item.Quantity));
                    }
                    else
                    {
                        // For regular sales: SUBTRACT quantity from stock (positive change)
                        stockUpdates.Add((item.ArticleId, item.Quantity));
                    }
                }

                if (stockUpdates.Any())
                {
                    await _articleService.UpdateStockQuantitiesAsync(stockUpdates);
                }
            }

            return sale;
        }

        public async Task<bool> UpdateSaleAsync(SalesInvoice sale, string? userId = null)
        {
            if (sale.IsPosted)
            {
                throw new InvalidOperationException("Posted sales cannot be modified");
            }

            var query = _context.SalesInvoices.Include(s => s.Items).AsQueryable();

            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(s => s.CreatedByUserId == userId);
            }

            var existingSale = await query.FirstOrDefaultAsync(s => s.Id == sale.Id);

            if (existingSale == null)
            {
                return false;
            }

            if (sale.IsReturn && sale.Items != null)
            {
                var validation = await ValidateReturnQuantitiesAsync(sale.Items.ToList());
                if (!validation.isValid)
                {
                    throw new InvalidOperationException(validation.errorMessage);
                }
            }

            // Calculate stock adjustments BEFORE updating the database
            var stockAdjustments = new List<(int articleId, decimal quantityChange)>();

            // Handle removed items (restore their quantities by adding them back)
            var existingItemIds = existingSale.Items.Select(i => i.Id).ToList();
            var updatedItemIds = sale.Items?.Where(i => i.Id > 0).Select(i => i.Id).ToList() ?? new List<int>();
            var itemsToRemove = existingSale.Items.Where(i => !updatedItemIds.Contains(i.Id)).ToList();
            
            foreach (var itemToRemove in itemsToRemove)
            {
                if (existingSale.IsReturn)
                {
                    // For returns being removed: subtract the quantity back (it was added to stock originally)
                    stockAdjustments.Add((itemToRemove.ArticleId, itemToRemove.Quantity));
                }
                else
                {
                    // For regular sales being removed: add back the quantity (restore stock)
                    stockAdjustments.Add((itemToRemove.ArticleId, -itemToRemove.Quantity));
                }
                _context.SalesInvoiceItems.Remove(itemToRemove);
            }

            // Handle updated and new items
            if (sale.Items != null)
            {
                foreach (var item in sale.Items)
                {
                    if (item.Id > 0)
                    {
                        // Existing item - check if quantity changed
                        var existingItem = existingSale.Items.FirstOrDefault(i => i.Id == item.Id);
                        if (existingItem != null && existingItem.Quantity != item.Quantity)
                        {
                            // Calculate the difference in quantities
                            var quantityDifference = item.Quantity - existingItem.Quantity;
                            if (quantityDifference != 0)
                            {
                                if (sale.IsReturn)
                                {
                                    // For returns: if quantity increased, add more to stock (negative diff)
                                    // if quantity decreased, remove from stock (positive diff)
                                    stockAdjustments.Add((item.ArticleId, -quantityDifference));
                                }
                                else
                                {
                                    // For regular sales: if quantity increased, subtract more from stock
                                    // if quantity decreased, add back to stock
                                    stockAdjustments.Add((item.ArticleId, quantityDifference));
                                }
                            }
                        }
                        
                        if (existingItem != null)
                        {
                            _context.Entry(existingItem).CurrentValues.SetValues(item);
                        }
                    }
                    else
                    {
                        // New item
                        item.SalesInvoiceId = sale.Id;
                        existingSale.Items.Add(item);
                        
                        if (sale.IsReturn)
                        {
                            // For new return items: add quantity to stock
                            stockAdjustments.Add((item.ArticleId, -item.Quantity));
                        }
                        else
                        {
                            // For new regular sale items: subtract quantity from stock
                            stockAdjustments.Add((item.ArticleId, item.Quantity));
                        }
                    }
                }
            }

            // Update sale totals
            sale.LastModifiedAt = DateTime.UtcNow;
            sale.LastModifiedByUserId = userId;

            sale.TotalWithoutVAT = sale.Items?.Sum(item => item.ValueWithoutVAT) ?? 0;
            sale.TotalVATAmount = sale.Items?.Sum(item => item.VATAmount) ?? 0;
            sale.TotalWithVAT = sale.Items?.Sum(item => item.ValueWithVAT) ?? 0;
            sale.TotalDiscountAmount = sale.Items?.Sum(item => item.DiscountAmount) ?? 0;

            // Update sale properties (preserve invoice number)
            var originalInvoiceNumber = existingSale.InvoiceNumber;
            _context.Entry(existingSale).CurrentValues.SetValues(sale);
            existingSale.InvoiceNumber = originalInvoiceNumber;

            try
            {
                // Save the invoice changes first
                await _context.SaveChangesAsync();

                // Apply stock adjustments if any
                if (stockAdjustments.Any())
                {
                    await _articleService.UpdateStockQuantitiesAsync(stockAdjustments);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating invoice: {ex.Message}");
                return false;
            }
        }

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

        public async Task<bool> CancelSaleAsync(int id, string reason, string userId)
        {
            var sale = await _context.SalesInvoices
                .Include(s => s.Items)
                .FirstOrDefaultAsync(s => s.Id == id && s.CreatedByUserId == userId);
            
            if (sale == null)
            {
                return false;
            }

            if (sale.IsPosted && sale.Items != null && sale.Items.Any())
            {
                var stockAdjustments = new List<(int articleId, decimal quantityChange)>();

                foreach (var item in sale.Items.Where(i => i.ArticleId > 0))
                {
                    if (sale.IsReturn)
                    {
                        // For cancelled returns: remove the quantity from stock (it was added back originally)
                        stockAdjustments.Add((item.ArticleId, item.Quantity));
                    }
                    else
                    {
                        // For cancelled regular sales: add the quantity back to stock
                        stockAdjustments.Add((item.ArticleId, -item.Quantity));
                    }
                }

                if (stockAdjustments.Any())
                {
                    await _articleService.UpdateStockQuantitiesAsync(stockAdjustments);
                }
            }

            sale.IsCancelled = true;
            sale.CancellationReason = reason;
            sale.LastModifiedByUserId = userId;
            sale.LastModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<SalesInvoice>> SearchSalesAsync(string searchTerm, int? businessUnitId = null, string? userId = null)
        {
            var query = _context.SalesInvoices.AsQueryable();

            if (businessUnitId.HasValue)
            {
                query = query.Where(s => s.BusinessUnitId == businessUnitId.Value);
            }

            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(s => s.CreatedByUserId == userId);
            }

            return await query
                .Where(s => s.InvoiceNumber.Contains(searchTerm) ||
                           s.BuyerName.Contains(searchTerm) ||
                           (s.OriginalInvoiceNumber != null && s.OriginalInvoiceNumber.Contains(searchTerm)))
                .Include(s => s.Buyer)
                .Include(s => s.OriginalInvoice)
                .Include(s => s.Items)
                    .ThenInclude(item => item.Article)
                .OrderByDescending(s => s.InvoiceDate)
                .ToListAsync();
        }
    }
}