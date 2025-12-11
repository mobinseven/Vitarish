namespace Vitarish.Clients.Interface.Identity.Abstractions
{
    public interface IUserProfileCultureInfoProvider
    {
        Task<CultureInfo?> GetRequestedCultureAsync();
        Task<bool> SetCultureAsync(string cultureName);
    }
}