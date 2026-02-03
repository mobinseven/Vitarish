using System.Globalization;

namespace Vitarish.Interfaces.Accounts.Abstractions
{
    public interface IUserProfileCultureInfoProvider
    {
        Task<CultureInfo?> GetRequestedCultureAsync();
        Task SetCultureAsync(string cultureName);
    }
}
