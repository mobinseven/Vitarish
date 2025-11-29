using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Localization;
using Vorn.Aaas.Interface.Infrastructure.Localization;

namespace Vorn.Aaas.Interface.Tests.TestInfrastructure;

internal sealed class TestStringLocalizer : IStringLocalizer<Strings>, IEnumerable<LocalizedString>
{
    public LocalizedString this[string name]
    {
        get
        {
            string value = name;
            return new LocalizedString(name, value, resourceNotFound: false);
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            string formatted = arguments.Length == 0
                ? name
                : $"{name} ({string.Join(", ", arguments)})";
            return new LocalizedString(name, formatted, resourceNotFound: false);
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => Enumerable.Empty<LocalizedString>();

    public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture) => this;

    IEnumerator<LocalizedString> IEnumerable<LocalizedString>.GetEnumerator() => Enumerable.Empty<LocalizedString>().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<LocalizedString>)this).GetEnumerator();
}
