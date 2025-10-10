using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;

namespace Server.Services
{
    public class LocalizationService : ILocalizationService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private int? _cachedDefaultLanguageId;
        private string _currentLanguageCode = "sq"; // Default to Albanian
        private int _currentLanguageId = 2; // Default to Albanian
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        public LocalizationService(IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public string CurrentLanguageCode => _currentLanguageCode;
        public int CurrentLanguageId => _currentLanguageId;

        public string? GetString(string key, int languageId)
        {
            using var context = _contextFactory.CreateDbContext();
            return context.LocalizationStrings
                .Where(ls => ls.StringKey == key && ls.LanguageId == languageId)
                .Select(ls => ls.Text)
                .FirstOrDefault();
        }

        public async Task<int> GetDefaultLanguageId()
        {
            if (_cachedDefaultLanguageId.HasValue)
                return _cachedDefaultLanguageId.Value;

            await _semaphore.WaitAsync();
            try
            {
                if (_cachedDefaultLanguageId.HasValue)
                    return _cachedDefaultLanguageId.Value;

                using var context = await _contextFactory.CreateDbContextAsync();
                var defaultLanguage = await context.Languages
                    .FirstOrDefaultAsync(l => l.IsDefault)
                    ?? throw new InvalidOperationException("No default language set");

                _cachedDefaultLanguageId = defaultLanguage.LanguageId;
                return defaultLanguage.LanguageId;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<string> GetStringAsync(string key)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var languageId = await GetDefaultLanguageId();
            return await GetStringAsync(key, languageId.ToString());
        }

        public async Task<string> GetStringAsync(string key, string languageCode)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var language = await context.Languages
                .FirstOrDefaultAsync(l => l.Code == languageCode);
            
            if (language == null)
                return key;

            var locString = await context.LocalizationStrings
                .FirstOrDefaultAsync(ls => ls.StringKey == key && ls.LanguageId == language.LanguageId);

            return locString?.Text ?? key;
        }

        public async Task<Dictionary<string, string>> GetAllStringsAsync()
        {
            var languageId = await GetDefaultLanguageId();
            return await GetAllStringsForLanguageId(languageId);
        }

        public async Task<Dictionary<string, string>> GetAllStringsAsync(string languageCode)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var language = await context.Languages
                .FirstOrDefaultAsync(l => l.Code == languageCode);
            
            if (language == null)
                return new Dictionary<string, string>();

            return await GetAllStringsForLanguageId(language.LanguageId);
        }

        private async Task<Dictionary<string, string>> GetAllStringsForLanguageId(int languageId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.LocalizationStrings
                .Where(ls => ls.LanguageId == languageId)
                .ToDictionaryAsync(ls => ls.StringKey, ls => ls.Text);
        }

        public async Task<List<Language>> GetAvailableLanguagesAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Languages
                .Where(l => l.IsActive)
                .OrderBy(l => l.Name)
                .ToListAsync();
        }

        public async Task<Language> GetCurrentLanguageAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Languages
                .FirstOrDefaultAsync(l => l.Code == _currentLanguageCode)
                ?? await context.Languages.FirstAsync(l => l.IsDefault);
        }

        public async Task<bool> SetLanguageAsync(string languageCode)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var language = await context.Languages
                .FirstOrDefaultAsync(l => l.Code == languageCode && l.IsActive);
                
            if (language == null)
                return false;

            _currentLanguageCode = languageCode;
            _currentLanguageId = language.LanguageId;
            return true;
        }

        public async Task<bool> SetLanguageAsync(int languageId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var language = await context.Languages
                .FirstOrDefaultAsync(l => l.LanguageId == languageId && l.IsActive);

            if (language == null)
                return false;

            _currentLanguageId = languageId;
            _currentLanguageCode = language.Code;
            return true;
        }
    }
}