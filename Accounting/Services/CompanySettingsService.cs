using Microsoft.EntityFrameworkCore;
using Accounting.Data;
using Accounting.Models;

namespace Accounting.Services
{
    public class CompanySettingsService
    {
        private readonly ApplicationDbContext _context;

        public CompanySettingsService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<CompanySettings?> GetSettingsAsync()
        {
            return await _context.CompanySettings
                .Include(cs => cs.DefaultCurrency)
                .Include(cs => cs.DefaultLanguage)
                .Include(cs => cs.LastModifiedByUser)
                .FirstOrDefaultAsync();
        }

        public async Task<CompanySettings> GetOrCreateSettingsAsync()
        {
            var settings = await GetSettingsAsync();

            if (settings == null)
            {
                // Create default settings
                settings = new CompanySettings
                {
                    CompanyName = "My Company",
                    DefaultCurrencyId = 1, // EUR by default
                    DefaultLanguageId = 1, // English by default
                    FiscalYearStartMonth = 1,
                    FiscalYearStartDay = 1,
                    DateFormat = "dd/MM/yyyy",
                    NumberFormat = "0.00",
                    CreatedAt = DateTime.UtcNow
                };

                _context.CompanySettings.Add(settings);
                await _context.SaveChangesAsync();
            }

            return settings;
        }

        public async Task<bool> UpdateSettingsAsync(CompanySettings settings, string userId)
        {
            var existingSettings = await GetSettingsAsync();

            if (existingSettings == null)
            {
                settings.CreatedAt = DateTime.UtcNow;
                settings.LastModifiedByUserId = userId;
                _context.CompanySettings.Add(settings);
            }
            else
            {
                existingSettings.CompanyName = settings.CompanyName;
                existingSettings.TradeName = settings.TradeName;
                existingSettings.LogoPath = settings.LogoPath;
                existingSettings.VATNumber = settings.VATNumber;
                existingSettings.BusinessRegistrationNumber = settings.BusinessRegistrationNumber;
                existingSettings.NUI = settings.NUI;
                existingSettings.Address = settings.Address;
                existingSettings.City = settings.City;
                existingSettings.PostalCode = settings.PostalCode;
                existingSettings.Country = settings.Country;
                existingSettings.PhoneNumber = settings.PhoneNumber;
                existingSettings.Email = settings.Email;
                existingSettings.Website = settings.Website;
                existingSettings.TaxOffice = settings.TaxOffice;
                existingSettings.DefaultCurrencyId = settings.DefaultCurrencyId;
                existingSettings.FiscalYearStartMonth = settings.FiscalYearStartMonth;
                existingSettings.FiscalYearStartDay = settings.FiscalYearStartDay;
                existingSettings.BankAccountDetails = settings.BankAccountDetails;
                existingSettings.IBAN = settings.IBAN;
                existingSettings.SwiftBicCode = settings.SwiftBicCode;
                existingSettings.BankName = settings.BankName;
                existingSettings.InvoiceFooterText = settings.InvoiceFooterText;
                existingSettings.TermsAndConditions = settings.TermsAndConditions;
                existingSettings.DateFormat = settings.DateFormat;
                existingSettings.NumberFormat = settings.NumberFormat;
                existingSettings.DefaultLanguageId = settings.DefaultLanguageId;
                existingSettings.UpdatedAt = DateTime.UtcNow;
                existingSettings.LastModifiedByUserId = userId;

                _context.CompanySettings.Update(existingSettings);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<Currency>> GetAllCurrenciesAsync()
        {
            return await _context.Currencies.OrderBy(c => c.Code).ToListAsync();
        }

        public async Task<List<Language>> GetAllLanguagesAsync()
        {
            return await _context.Languages
                .Where(l => l.IsActive)
                .OrderBy(l => l.Name)
                .ToListAsync();
        }
    }
}