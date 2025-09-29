using Server.Models;

namespace Server.Services
{
    public interface ILocalizationService
    {
        string? GetString(string key, int languageId);
        Task<Dictionary<string, string>> GetAllStringsAsync();
        Task<Dictionary<string, string>> GetAllStringsAsync(string languageCode);
        Task<List<Language>> GetAvailableLanguagesAsync();
        Task<Language> GetCurrentLanguageAsync();
        Task<bool> SetLanguageAsync(int languageId);
        Task<int> GetDefaultLanguageId();
        int CurrentLanguageId { get; }
    }
}