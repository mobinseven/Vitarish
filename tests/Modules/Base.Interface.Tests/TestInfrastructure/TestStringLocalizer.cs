using Microsoft.Extensions.Localization;

namespace Modules.Base.Interface.Tests.TestInfrastructure;

public sealed class TestStringLocalizer(IReadOnlyDictionary<string, string> resources) : IStringLocalizer
{
    public LocalizedString this[string name] => resources.TryGetValue(name, out string? value)
                ? new LocalizedString(name, value, resourceNotFound: false)
                : new LocalizedString(name, name, resourceNotFound: true);

    public LocalizedString this[string name, params object[] arguments] => resources.TryGetValue(name, out string? value)
                ? new LocalizedString(name, string.Format(CultureInfo.CurrentCulture, value, arguments), resourceNotFound: false)
                : new LocalizedString(name, string.Format(CultureInfo.CurrentCulture, name, arguments), resourceNotFound: true);

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        => resources.Select(entry => new LocalizedString(entry.Key, entry.Value, resourceNotFound: false));
}
