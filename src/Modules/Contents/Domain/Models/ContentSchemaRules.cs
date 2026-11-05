using System.Collections.Immutable;
using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Contents.Domain.Models;

public static class ContentSchemaRules
{
    public const int MaximumFieldCount = 100;
    public const int MaximumDepth = 3;

    public static IReadOnlyList<EntityError> Validate(IReadOnlyList<ContentFieldDefinition> fields)
    {
        ArgumentNullException.ThrowIfNull(fields);

        List<EntityError> errors = [];
        int definitionCount = 0;
        ValidateSiblings(fields, ContentFieldsRules.RootTarget, 1, errors, ref definitionCount);
        if (definitionCount > MaximumFieldCount)
        {
            errors.Add(EntityError.Validation($"A content schema can contain at most {MaximumFieldCount} fields.", ContentFieldsRules.RootTarget));
        }

        return errors;
    }

    public static IReadOnlyList<EntityError> EnsureAdditive(
        IReadOnlyList<ContentFieldDefinition> previous,
        IReadOnlyList<ContentFieldDefinition> next)
    {
        ArgumentNullException.ThrowIfNull(previous);
        ArgumentNullException.ThrowIfNull(next);

        List<EntityError> errors = [.. Validate(previous), .. Validate(next)];
        if (errors.Count > 0)
        {
            return errors;
        }

        EnsureExistingFieldsRemain(previous, next, ContentFieldsRules.RootTarget, errors);
        EnsureNewFieldsAreOptional(previous, next, ContentFieldsRules.RootTarget, errors);
        return errors;
    }

    private static void ValidateSiblings(
        IReadOnlyList<ContentFieldDefinition> fields,
        string path,
        int depth,
        List<EntityError> errors,
        ref int definitionCount)
    {
        HashSet<string> keys = new(StringComparer.Ordinal);
        for (int index = 0; index < fields.Count; index++)
        {
            ContentFieldDefinition? field = fields[index];
            string fieldPath = $"{path}[{index}]";
            definitionCount++;

            if (field is null)
            {
                errors.Add(EntityError.Validation("Field definition cannot be null.", fieldPath));
                continue;
            }

            if (!ContentKeyRules.IsCamelCase(field.Key))
            {
                errors.Add(EntityError.Validation("Field key must be a camelCase slug.", $"{fieldPath}.key"));
            }
            else if (!keys.Add(field.Key))
            {
                errors.Add(EntityError.Validation("Field keys must be unique within their group.", $"{fieldPath}.key"));
            }

            if (string.IsNullOrWhiteSpace(field.Label))
            {
                errors.Add(EntityError.Validation("Field label is required.", $"{fieldPath}.label"));
            }

            if (!Enum.IsDefined(field.Kind))
            {
                errors.Add(EntityError.Validation("Field kind is invalid.", $"{fieldPath}.kind"));
            }

            if (depth > MaximumDepth)
            {
                errors.Add(EntityError.Validation($"Content schema nesting cannot exceed {MaximumDepth} levels.", fieldPath));
                continue;
            }

            if (field.MaxLength is <= 0 || field.MaxLength is not null && field.Kind is not (ContentFieldKind.Text or ContentFieldKind.LongText))
            {
                errors.Add(EntityError.Validation("MaxLength must be positive and is supported only for text fields.", $"{fieldPath}.maxLength"));
            }

            if (field.AllowedTypeKeys is { } allowedTypeKeys)
            {
                if (field.Kind != ContentFieldKind.Reference)
                {
                    errors.Add(EntityError.Validation("Allowed type keys are supported only for reference fields.", $"{fieldPath}.allowedTypeKeys"));
                }

                HashSet<string> allowedKeys = new(StringComparer.Ordinal);
                foreach (string typeKey in GetAllowedTypeKeys(allowedTypeKeys))
                {
                    if (!ContentKeyRules.IsSlug(typeKey))
                    {
                        errors.Add(EntityError.Validation("Allowed type key must be a lowercase slug.", $"{fieldPath}.allowedTypeKeys"));
                    }
                    else if (!allowedKeys.Add(typeKey))
                    {
                        errors.Add(EntityError.Validation("Allowed type keys must be unique.", $"{fieldPath}.allowedTypeKeys"));
                    }
                }
            }
            if (field.Kind == ContentFieldKind.List)
            {
                if (field.Item is null)
                {
                    errors.Add(EntityError.Validation("List fields require an item definition.", $"{fieldPath}.item"));
                }
                else
                {
                    ValidateSiblings([field.Item], $"{fieldPath}.item", depth + 1, errors, ref definitionCount);
                }
            }
            else if (field.Item is not null)
            {
                errors.Add(EntityError.Validation("Item is supported only for list fields.", $"{fieldPath}.item"));
            }

            if (field.Kind == ContentFieldKind.Group)
            {
                ValidateSiblings(GetFields(field.Fields), $"{fieldPath}.fields", depth + 1, errors, ref definitionCount);
            }
            else if (!field.Fields.IsDefaultOrEmpty)
            {
                errors.Add(EntityError.Validation("Child fields are supported only for group fields.", $"{fieldPath}.fields"));
            }
        }
    }

    private static void EnsureExistingFieldsRemain(
        IReadOnlyList<ContentFieldDefinition> previous,
        IReadOnlyList<ContentFieldDefinition> next,
        string path,
        List<EntityError> errors)
    {
        Dictionary<string, ContentFieldDefinition> nextByKey = next.ToDictionary(field => field.Key, StringComparer.Ordinal);
        foreach (ContentFieldDefinition previousField in previous)
        {
            string fieldPath = $"{path}.{previousField.Key}";
            if (!nextByKey.TryGetValue(previousField.Key, out ContentFieldDefinition? nextField))
            {
                errors.Add(EntityError.Validation("Existing fields cannot be removed.", fieldPath));
                continue;
            }

            EnsureDefinitionRemainsCompatible(previousField, nextField, fieldPath, errors);
        }
    }

    private static void EnsureDefinitionRemainsCompatible(
        ContentFieldDefinition previous,
        ContentFieldDefinition next,
        string path,
        List<EntityError> errors)
    {
        if (!string.Equals(previous.Key, next.Key, StringComparison.Ordinal))
        {
            errors.Add(EntityError.Validation("Existing field keys cannot change.", $"{path}.key"));
        }

        if (previous.Kind != next.Kind)
        {
            errors.Add(EntityError.Validation("Existing field kinds cannot change.", $"{path}.kind"));
        }

        if (previous.Localized != next.Localized)
        {
            errors.Add(EntityError.Validation("Existing field localization cannot change.", $"{path}.localized"));
        }

        if (!previous.Required && next.Required)
        {
            errors.Add(EntityError.Validation("An existing optional field cannot become required.", $"{path}.required"));
        }

        if (previous.MaxLength is null && next.MaxLength is not null ||
            previous.MaxLength is int previousMaximum && next.MaxLength is int nextMaximum && nextMaximum < previousMaximum)
        {
            errors.Add(EntityError.Validation("Existing field length limits may only be relaxed.", $"{path}.maxLength"));
        }

        bool previousRestricted = previous.AllowedTypeKeys is { IsDefaultOrEmpty: false };
        bool nextRestricted = next.AllowedTypeKeys is { IsDefaultOrEmpty: false };
        if (!previousRestricted && nextRestricted)
        {
            errors.Add(EntityError.Validation("Existing reference type limits may only be relaxed.", $"{path}.allowedTypeKeys"));
        }
        else if (previousRestricted && nextRestricted &&
            previous.AllowedTypeKeys is { } previousTypeKeys && next.AllowedTypeKeys is { } nextTypeKeys)
        {
            HashSet<string> nextTypeKeySet = new(GetAllowedTypeKeys(nextTypeKeys), StringComparer.Ordinal);
            if (GetAllowedTypeKeys(previousTypeKeys).Any(typeKey => !nextTypeKeySet.Contains(typeKey)))
            {
                errors.Add(EntityError.Validation("Existing reference type limits may only be relaxed.", $"{path}.allowedTypeKeys"));
            }
        }

        if (previous.Kind == ContentFieldKind.Group)
        {
            EnsureExistingFieldsRemain(GetFields(previous.Fields), GetFields(next.Fields), $"{path}.fields", errors);
            EnsureNewFieldsAreOptional(GetFields(previous.Fields), GetFields(next.Fields), $"{path}.fields", errors);
        }

        if (previous.Kind == ContentFieldKind.List && previous.Item is not null && next.Item is not null)
        {
            EnsureDefinitionRemainsCompatible(previous.Item, next.Item, $"{path}.item", errors);
        }
    }

    private static void EnsureNewFieldsAreOptional(
        IReadOnlyList<ContentFieldDefinition> previous,
        IReadOnlyList<ContentFieldDefinition> next,
        string path,
        List<EntityError> errors)
    {
        HashSet<string> previousKeys = previous.Select(field => field.Key).ToHashSet(StringComparer.Ordinal);
        foreach (ContentFieldDefinition field in next)
        {
            if (!previousKeys.Contains(field.Key) && !IsEntirelyOptional(field))
            {
                errors.Add(EntityError.Validation("New schema fields and their nested fields must be optional.", $"{path}.{field.Key}"));
            }
        }
    }

    private static bool IsEntirelyOptional(ContentFieldDefinition field) =>
        !field.Required &&
        GetFields(field.Fields).All(IsEntirelyOptional) &&
        (field.Item is null || IsEntirelyOptional(field.Item));

    private static IReadOnlyList<ContentFieldDefinition> GetFields(ImmutableArray<ContentFieldDefinition> fields) =>
        fields.IsDefault ? Array.Empty<ContentFieldDefinition>() : fields;

    private static IEnumerable<string> GetAllowedTypeKeys(ImmutableArray<string> keys) =>
        keys.IsDefault ? Array.Empty<string>() : keys;
}
