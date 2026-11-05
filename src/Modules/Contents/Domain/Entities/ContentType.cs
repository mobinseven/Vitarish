using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Contents.Domain.Entities;

public class ContentType : Entity
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CurrentSchemaVersion { get; set; }

    public IReadOnlyList<EntityError> Validate()
    {
        List<EntityError> errors = [];

        if (!ContentKeyRules.IsSlug(Key))
        {
            errors.Add(EntityError.Validation("Content type key must be a lowercase slug.", nameof(Key)));
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            errors.Add(EntityError.Validation("Content type name is required.", nameof(Name)));
        }

        if (CurrentSchemaVersion < 0)
        {
            errors.Add(EntityError.Validation("Current schema version cannot be negative.", nameof(CurrentSchemaVersion)));
        }

        return errors;
    }
}
