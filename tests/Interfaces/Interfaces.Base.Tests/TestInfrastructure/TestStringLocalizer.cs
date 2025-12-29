using Microsoft.Extensions.Localization;
using System.Globalization;

namespace Interfaces.Base.Tests.TestInfrastructure;

public sealed class TestStringLocalizer : IStringLocalizer
{
    private readonly IReadOnlyDictionary<string, string> _resources;

    public TestStringLocalizer(IReadOnlyDictionary<string, string> resources)
    {
        _resources = resources;
    }

    public LocalizedString this[string name]
    {
        get
        {
            if (_resources.TryGetValue(name, out string? value))
            {
                return new LocalizedString(name, value, resourceNotFound: false);
            }

            return new LocalizedString(name, name, resourceNotFound: true);
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            if (_resources.TryGetValue(name, out string? value))
            {
                return new LocalizedString(name, string.Format(CultureInfo.CurrentCulture, value, arguments), resourceNotFound: false);
            }

            return new LocalizedString(name, string.Format(CultureInfo.CurrentCulture, name, arguments), resourceNotFound: true);
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        => _resources.Select(entry => new LocalizedString(entry.Key, entry.Value, resourceNotFound: false));
}
