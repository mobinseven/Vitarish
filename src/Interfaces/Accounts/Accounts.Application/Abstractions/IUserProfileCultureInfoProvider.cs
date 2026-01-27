using System.Globalization;

namespace Vitarish.Accounts.Application.Abstractions
{
    public interface IUserProfileCultureInfoProvider
    {
        Task<CultureInfo?> GetRequestedCultureAsync();
        Task SetCultureAsync(string cultureName);
    }
}
