using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;

namespace Server.Services
{
    public class InvoiceNumberService
    {
        private readonly ApplicationDbContext _context;

        public InvoiceNumberService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<InvoiceNumberFormat?> GetCurrentFormatAsync()
        {
            return await _context.InvoiceNumberFormats
                .OrderByDescending(f => f.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task SaveFormatAsync(InvoiceNumberFormat format)
        {
            if (format.Id == 0)
            {
                _context.InvoiceNumberFormats.Add(format);
            }
            else
            {
                format.LastModifiedAt = DateTime.UtcNow;
                _context.InvoiceNumberFormats.Update(format);
            }

            await _context.SaveChangesAsync();
        }

        public async Task<string> GenerateInvoiceNumberAsync(int businessUnitId, int salesCategoryId)
        {
            var format = await GetCurrentFormatAsync();
            if (format == null)
                throw new InvalidOperationException("Invoice number format not configured");

            var parts = new List<string>();

            if (format.UseYear)
                parts.Add(DateTime.Now.ToString("yy"));

            if (format.UseSalesCategoryCode)
            {
                var category = await _context.SalesCategories.FindAsync(salesCategoryId);
                parts.Add(category?.Code ?? "UNK");
            }

            if (format.UseBusinessUnitCode)
            {
                var unit = await _context.BusinessUnits.FindAsync(businessUnitId);
                parts.Add(unit?.Code.PadLeft(3, '0') ?? "000");
            }

            if (format.UseSequentialNumber)
            {
                var lastNumber = await GetLastSequentialNumberAsync(businessUnitId, salesCategoryId);
                parts.Add((lastNumber + 1).ToString().PadLeft(format.SequentialNumberLength, '0'));
            }

            return string.Join(format.Separator, parts);
        }

        private async Task<int> GetLastSequentialNumberAsync(int businessUnitId, int salesCategoryId)
        {
            var lastInvoice = await _context.SalesInvoices
                .Where(si => si.BusinessUnitId == businessUnitId && si.SalesCategoryId == salesCategoryId)
                .OrderByDescending(si => si.Id)
                .FirstOrDefaultAsync();

            return lastInvoice?.SequentialNumber ?? 0;
        }
    }
}