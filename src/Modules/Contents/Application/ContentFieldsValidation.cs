using System.Collections.Immutable;
using System.Text.Json;
using Vitarish.Contents.Application.Abstractions;
using Vitarish.Contents.Domain.Entities;
using Vitarish.Contents.Domain.Enums;
using Vitarish.Contents.Domain.Models;
using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Contents.Application;

internal static class ContentFieldsValidation
{
    public static async Task<IReadOnlyList<EntityError>> ValidateAsync(
        IContentAuthoringStore store,
        string tenantId,
        IReadOnlyList<ContentFieldDefinition> schema,
        JsonElement fields,
        IReadOnlyCollection<string> siteLocales,
        ContentFieldsValidationMode mode,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<EntityError> errors = ContentFieldsRules.Validate(schema, fields, siteLocales, mode);
        if (errors.Count > 0)
        {
            return errors;
        }

        List<(Guid ContentId, ImmutableArray<string>? AllowedTypeKeys, string Target)> references = [];
        CollectReferences(schema, fields, ContentFieldsRules.RootTarget, references);
        List<EntityError> referenceErrors = [];
        foreach ((Guid referencedContentId, ImmutableArray<string>? allowedTypeKeys, string target) in references)
        {
            ContentItem? referencedItem = await store.GetEntryByIdAsync(tenantId, referencedContentId, cancellationToken);
            if (referencedItem?.ContentTypeId is not Guid referencedTypeId)
            {
                referenceErrors.Add(EntityError.Validation("Referenced content must exist in this tenant.", target));
                continue;
            }

            if (!allowedTypeKeys.HasValue || allowedTypeKeys.Value.IsDefaultOrEmpty)
            {
                continue;
            }

            ContentType? referencedType = await store.GetContentTypeByIdAsync(tenantId, referencedTypeId, cancellationToken);
            if (referencedType is null || !allowedTypeKeys.Value.Contains(referencedType.Key, StringComparer.Ordinal))
            {
                referenceErrors.Add(EntityError.Validation("Referenced content type is not allowed by this field.", target));
            }
        }

        return referenceErrors;
    }

    private static void CollectReferences(
        IReadOnlyList<ContentFieldDefinition> definitions,
        JsonElement value,
        string target,
        List<(Guid ContentId, ImmutableArray<string>? AllowedTypeKeys, string Target)> references)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (ContentFieldDefinition definition in definitions)
        {
            if (!value.TryGetProperty(definition.Key, out JsonElement fieldValue))
            {
                continue;
            }

            CollectFieldReference(definition, fieldValue, $"{target}.{definition.Key}", references);
        }
    }

    private static void CollectFieldReference(
        ContentFieldDefinition definition,
        JsonElement value,
        string target,
        List<(Guid ContentId, ImmutableArray<string>? AllowedTypeKeys, string Target)> references)
    {
        if (definition.Localized && value.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty localizedValue in value.EnumerateObject())
            {
                CollectFieldValue(definition, localizedValue.Value, $"{target}.{localizedValue.Name}", references);
            }

            return;
        }

        CollectFieldValue(definition, value, target, references);
    }

    private static void CollectFieldValue(
        ContentFieldDefinition definition,
        JsonElement value,
        string target,
        List<(Guid ContentId, ImmutableArray<string>? AllowedTypeKeys, string Target)> references)
    {
        switch (definition.Kind)
        {
            case ContentFieldKind.Reference:
                if (value.ValueKind == JsonValueKind.String && Guid.TryParse(value.GetString(), out Guid contentId))
                {
                    references.Add((contentId, definition.AllowedTypeKeys, target));
                }

                break;
            case ContentFieldKind.Group:
                CollectReferences(definition.Fields.IsDefault ? [] : definition.Fields, value, target, references);
                break;
            case ContentFieldKind.List:
                if (definition.Item is not null && value.ValueKind == JsonValueKind.Array)
                {
                    int index = 0;
                    foreach (JsonElement itemValue in value.EnumerateArray())
                    {
                        CollectFieldReference(definition.Item, itemValue, $"{target}[{index}]", references);
                        index++;
                    }
                }

                break;
        }
    }
}
