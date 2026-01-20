namespace Vitarish.Interfaces.Base.Localization;

[RegisterSingleton]
public class StringLocalizer(IStringLocalizer<Strings> localizer) : IStringLocalizer
{
    public LocalizedString this[string name] => localizer[name ?? string.Empty];

    public LocalizedString this[string name, params object[] arguments] => localizer[name ?? string.Empty, arguments];

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => localizer.GetAllStrings(includeParentCultures);
}
