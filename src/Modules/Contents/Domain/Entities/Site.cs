using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Contents.Domain.Entities;

public class Site : Entity
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DefaultLocale { get; set; } = string.Empty;
    public string[] Locales { get; set; } = [];

    public IReadOnlyList<EntityError> Validate()
    {
        List<EntityError> errors = [];

        if (!ContentKeyRules.IsSlug(Key))
        {
            errors.Add(EntityError.Validation("Site key must be a lowercase slug.", nameof(Key)));
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            errors.Add(EntityError.Validation("Site name is required.", nameof(Name)));
        }

        if (Locales is null || Locales.Length == 0 || Locales.Any(string.IsNullOrWhiteSpace))
        {
            errors.Add(EntityError.Validation("At least one locale is required.", nameof(Locales)));
        }
        else
        {
            if (Locales.Distinct(StringComparer.Ordinal).Count() != Locales.Length)
            {
                errors.Add(EntityError.Validation("Locales must be unique.", nameof(Locales)));
            }

            if (!Locales.Contains(DefaultLocale, StringComparer.Ordinal))
            {
                errors.Add(EntityError.Validation("Default locale must be included in locales.", nameof(DefaultLocale)));
            }
        }

        if (string.IsNullOrWhiteSpace(DefaultLocale))
        {
            errors.Add(EntityError.Validation("Default locale is required.", nameof(DefaultLocale)));
        }

        return errors;
    }
}
