namespace Vitarish.Modules.Base.Core.Abstractions;

public interface IValidLocaleProvider
{
    Task<IReadOnlyList<string>> GetValidLocalesAsync(CancellationToken cancellationToken = default);
    Task<bool> IsValidLocaleAsync(string locale, CancellationToken cancellationToken = default);
}
