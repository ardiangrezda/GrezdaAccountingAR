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

        public async Task<SalesInvoice?> GetSaleByIdAsync(int id, string? userId = null)
        {
            var query = _context.SalesInvoices.AsQueryable();

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

           var (invoiceNumber, sequentialNumber) = await GenerateInvoiceNumberAsync(businessUnitId, sale.SalesCategoryId);
            
            sale.InvoiceNumber = invoiceNumber;
            sale.SequentialNumber = sequentialNumber;

            sale.TotalWithoutVAT = sale.Items?.Sum(item => item.ValueWithoutVAT) ?? 0;
            sale.TotalVATAmount = sale.Items?.Sum(item => item.VATAmount) ?? 0;
            sale.TotalWithVAT = sale.Items?.Sum(item => item.ValueWithVAT) ?? 0;
            sale.TotalDiscountAmount = sale.Items?.Sum(item => item.DiscountAmount) ?? 0;

            _context.SalesInvoices.Add(sale);
            await _context.SaveChangesAsync();

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

            sale.LastModifiedAt = DateTime.UtcNow;
            sale.LastModifiedByUserId = userId;

            sale.TotalWithoutVAT = sale.Items?.Sum(item => item.ValueWithoutVAT) ?? 0;
            sale.TotalVATAmount = sale.Items?.Sum(item => item.VATAmount) ?? 0;
            sale.TotalWithVAT = sale.Items?.Sum(item => item.ValueWithVAT) ?? 0;
            sale.TotalDiscountAmount = sale.Items?.Sum(item => item.DiscountAmount) ?? 0;

            var existingItemIds = existingSale.Items.Select(i => i.Id).ToList();
            var updatedItemIds = sale.Items?.Where(i => i.Id > 0).Select(i => i.Id).ToList() ?? new List<int>();
            var itemsToRemove = existingSale.Items.Where(i => !updatedItemIds.Contains(i.Id)).ToList();
            
            foreach (var itemToRemove in itemsToRemove)
            {
                _context.SalesInvoiceItems.Remove(itemToRemove);
            }

            if (sale.Items != null)
            {
                foreach (var item in sale.Items)
                {
                    if (item.Id > 0)
                    {
                        var existingItem = existingSale.Items.FirstOrDefault(i => i.Id == item.Id);
                        if (existingItem != null)
                        {
                            _context.Entry(existingItem).CurrentValues.SetValues(item);
                        }
                    }
                    else
                    {
                        item.SalesInvoiceId = sale.Id;
                        existingSale.Items.Add(item);
                    }
                }
            }

            var originalInvoiceNumber = existingSale.InvoiceNumber;
            _context.Entry(existingSale).CurrentValues.SetValues(sale);
            existingSale.InvoiceNumber = originalInvoiceNumber;

            try
            {
                await _context.SaveChangesAsync();
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
                           s.BuyerName.Contains(searchTerm))
                .Include(s => s.Buyer)
                .Include(s => s.Items)
                    .ThenInclude(item => item.Article)
                .OrderByDescending(s => s.InvoiceDate)
                .ToListAsync();
        }
    }
}