using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Server.Services
{
    public class LocalizationService : ILocalizationService
    {
        private readonly ApplicationDbContext _context;
        private int? _cachedDefaultLanguageId;
        private string _currentLanguageCode = "sq"; // Default to Albanian
        private int _currentLanguageId = 2; // Default to Albanian
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        // In-memory cache: languageId -> (key -> text)
        private readonly ConcurrentDictionary<int, Dictionary<string, string>> _cache = new();

        public LocalizationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public string CurrentLanguageCode => _currentLanguageCode;
        public int CurrentLanguageId => _currentLanguageId;

        // Read from in-memory cache only (safe for render paths)
        public string? GetString(string key, int languageId)
        {
            if (string.IsNullOrEmpty(key))
                return null;

            if (_cache.TryGetValue(languageId, out var dict) && dict != null)
            {
                return dict.TryGetValue(key, out var value) ? value : null;
            }

            // Cache miss: avoid performing synchronous DB access here to prevent DbContext concurrency issues.
            // Return null so callers can fall back to the key or other logic.
            return null;
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

                var defaultLanguage = await _context.Languages
                    .FirstOrDefaultAsync(l => l.IsDefault)
                    ?? throw new InvalidOperationException("No default language set");

                _cachedDefaultLanguageId = defaultLanguage.LanguageId;

                // Ensure default language cache is loaded before returning.
                await LoadLanguageCacheAsync(defaultLanguage.LanguageId).ConfigureAwait(false);

                return defaultLanguage.LanguageId;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<string> GetStringAsync(string key)
        {
            var languageId = await GetDefaultLanguageId();
            var text = GetString(key, languageId);
            if (text != null)
                return text;

            // Fallback to DB async if cache miss (does not reuse shared _context for concurrent access).
            var loc = await _context.LocalizationStrings
                .FirstOrDefaultAsync(ls => ls.StringKey == key && ls.LanguageId == languageId);

            return loc?.Text ?? key;
        }

        public async Task<string> GetStringAsync(string key, string languageCode)
        {
            // Try find language
            var language = await _context.Languages
                .FirstOrDefaultAsync(l => l.Code == languageCode);

            if (language == null)
                return key;

            var cached = GetString(key, language.LanguageId);
            if (cached != null)
                return cached;

            var locString = await _context.LocalizationStrings
                .FirstOrDefaultAsync(ls => ls.StringKey == key && ls.LanguageId == language.LanguageId);

            // Optionally populate cache for this language after DB read
            if (locString != null)
            {
                _cache.AddOrUpdate(language.LanguageId,
                    _ => new Dictionary<string, string> { [key] = locString.Text },
                    (_, existing) =>
                    {
                        existing[key] = locString.Text;
                        return existing;
                    });
            }

            return locString?.Text ?? key;
        }

        public async Task<Dictionary<string, string>> GetAllStringsAsync()
        {
            var languageId = await GetDefaultLanguageId();
            if (_cache.TryGetValue(languageId, out var dict) && dict != null)
                return dict;

            await LoadLanguageCacheAsync(languageId);
            return _cache.TryGetValue(languageId, out var loaded) ? loaded : new Dictionary<string, string>();
        }

        public async Task<Dictionary<string, string>> GetAllStringsAsync(string languageCode)
        {
            var language = await _context.Languages
                .FirstOrDefaultAsync(l => l.Code == languageCode);

            if (language == null)
                return new Dictionary<string, string>();

            if (_cache.TryGetValue(language.LanguageId, out var dict) && dict != null)
                return dict;

            await LoadLanguageCacheAsync(language.LanguageId);
            return _cache.TryGetValue(language.LanguageId, out var loaded) ? loaded : new Dictionary<string, string>();
        }

        private async Task LoadLanguageCacheAsync(int languageId)
        {
            // If cache already present, nothing to do.
            if (_cache.ContainsKey(languageId))
                return;

            // Read from DB using async EF call and populate cache atomically.
            var all = await _context.LocalizationStrings
                .Where(ls => ls.LanguageId == languageId)
                .ToDictionaryAsync(ls => ls.StringKey, ls => ls.Text);

            // Upsert into concurrent dictionary
            _cache.AddOrUpdate(languageId, all, (_, __) => all);
        }

        private async Task<Dictionary<string, string>> GetAllStringsForLanguageId(int languageId)
        {
            return await _context.LocalizationStrings
                .Where(ls => ls.LanguageId == languageId)
                .ToDictionaryAsync(ls => ls.StringKey, ls => ls.Text);
        }

        public async Task<List<Language>> GetAvailableLanguagesAsync()
        {
            return await _context.Languages
                .Where(l => l.IsActive)
                .OrderBy(l => l.Name)
                .ToListAsync();
        }

        public async Task<Language> GetCurrentLanguageAsync()
        {
            return await _context.Languages
                .FirstOrDefaultAsync(l => l.Code == _currentLanguageCode)
                ?? await _context.Languages.FirstAsync(l => l.IsDefault);
        }

        public async Task<bool> SetLanguageAsync(string languageCode)
        {
            var language = await _context.Languages
                .FirstOrDefaultAsync(l => l.Code == languageCode && l.IsActive);

            if (language == null)
                return false;

            _currentLanguageCode = languageCode;
            _currentLanguageId = language.LanguageId;

            // Ensure cache is available for chosen language
            await LoadLanguageCacheAsync(language.LanguageId).ConfigureAwait(false);

            return true;
        }

        public async Task<bool> SetLanguageAsync(int languageId)
        {
            var language = await _context.Languages
                .FirstOrDefaultAsync(l => l.LanguageId == languageId && l.IsActive);

            if (language == null)
                return false;

            _currentLanguageId = languageId;
            _currentLanguageCode = language.Code;

            // Ensure cache is available for chosen language
            await LoadLanguageCacheAsync(languageId).ConfigureAwait(false);

            return true;
        }
    }
}