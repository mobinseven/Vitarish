namespace Vitarish.Clients.Interface.Identity.Abstractions
{
    public interface IUserProfileClient
    {
        string? GetCulture(IDictionary<string, string> preferences);
        Task<string?> GetCultureAsync();
        Task<Dictionary<string, string>?> GetProfileAsync();
        void SetCulture(IDictionary<string, string> preferences, string culture);
        Task<bool> SetCultureAsync(string culture);
        Task UpdateProfileAsync(Dictionary<string, string> preferences);
    }
}