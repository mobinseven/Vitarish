using System.Globalization;

namespace Vorn.Aaas.Client.Abstractions
{
    public interface IUserProfileCultureInfoProvider
    {
        Task<CultureInfo?> GetRequestedCultureAsync();
        Task<bool> SetCultureAsync(string cultureName);
    }
}