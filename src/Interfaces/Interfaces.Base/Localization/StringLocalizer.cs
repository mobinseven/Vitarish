using System.Text;

namespace Vitarish.Interfaces.Base.Localization;

[RegisterSingleton]
public class StringLocalizer(IStringLocalizer<Strings> localizer) : IStringLocalizer
{
    public LocalizedString this[string name] => localizer.GetString(name);

    public LocalizedString this[string name, params object[] arguments] => localizer.GetString(name, arguments);

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => localizer.GetAllStrings(includeParentCultures);
}
