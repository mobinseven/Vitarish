namespace Vitarish.Interfaces.Base.Localization;

public sealed class CompositeStringLocalizer : IStringLocalizer
{
    private readonly IReadOnlyList<IStringLocalizer> innerLocalizers;

    public CompositeStringLocalizer(IEnumerable<IStringLocalizer> inner)
    {
        innerLocalizers = [.. inner];
    }

    public LocalizedString this[string name]
    {
        get
        {
            if (innerLocalizers.Count == 0)
            {
                return new LocalizedString(name, name, resourceNotFound: true);
            }

            foreach (IStringLocalizer localizer in innerLocalizers)
            {
                LocalizedString localized = localizer[name];
                if (!localized.ResourceNotFound)
                {
                    return localized;
                }
            }

            return innerLocalizers[0][name];
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            if (innerLocalizers.Count == 0)
            {
                return new LocalizedString(name, string.Format(CultureInfo.CurrentCulture, name, arguments), resourceNotFound: true);
            }

            foreach (IStringLocalizer localizer in innerLocalizers)
            {
                LocalizedString localized = localizer[name, arguments];
                if (!localized.ResourceNotFound)
                {
                    return localized;
                }
            }

            return innerLocalizers[0][name, arguments];
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        => innerLocalizers
            .SelectMany(localizer => localizer.GetAllStrings(includeParentCultures))
            .GroupBy(localized => localized.Name)
            .Select(group => group.First());
}
