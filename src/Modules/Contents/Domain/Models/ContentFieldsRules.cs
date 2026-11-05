using System.Globalization;
using System.Text.Json;
using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Contents.Domain.Models;

public static class ContentFieldsRules
{
    public const string RootTarget = "fields";
    private const string DateFormat = "yyyy-MM-dd";
    private const string UrlProperty = "url";
    private const string AltProperty = "alt";
    private const string LabelProperty = "label";

    public static IReadOnlyList<EntityError> Validate(
        IReadOnlyList<ContentFieldDefinition> schema,
        JsonElement fieldsJson,
        IReadOnlyCollection<string> siteLocales,
        ContentFieldsValidationMode mode)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(siteLocales);

        List<EntityError> errors = [.. ContentSchemaRules.Validate(schema)];
        if (errors.Count > 0)
        {
            return errors;
        }

        if (!Enum.IsDefined(mode))
        {
            errors.Add(EntityError.Validation("Content field validation mode is invalid.", nameof(mode)));
            return errors;
        }

        if (siteLocales.Count == 0 || siteLocales.Any(string.IsNullOrWhiteSpace))
        {
            errors.Add(EntityError.Validation("At least one site locale is required.", nameof(siteLocales)));
            return errors;
        }

        ValidateFieldsObject(schema, fieldsJson, RootTarget, siteLocales, mode, errors);
        return errors;
    }

    private static void ValidateFieldsObject(
        IReadOnlyList<ContentFieldDefinition> schema,
        JsonElement fieldsJson,
        string path,
        IReadOnlyCollection<string> siteLocales,
        ContentFieldsValidationMode mode,
        List<EntityError> errors)
    {
        if (fieldsJson.ValueKind != JsonValueKind.Object)
        {
            errors.Add(EntityError.Validation("Fields must be a JSON object.", path));
            return;
        }

        Dictionary<string, ContentFieldDefinition> definitions = schema.ToDictionary(field => field.Key, StringComparer.Ordinal);
        HashSet<string> present = new(StringComparer.Ordinal);
        foreach (JsonProperty property in fieldsJson.EnumerateObject())
        {
            string fieldPath = $"{path}.{property.Name}";
            if (!present.Add(property.Name))
            {
                errors.Add(EntityError.Validation("Field values cannot contain duplicate keys.", fieldPath));
                continue;
            }

            if (!definitions.TryGetValue(property.Name, out ContentFieldDefinition? field))
            {
                errors.Add(EntityError.Validation("Field is not defined by the content schema.", fieldPath));
                continue;
            }

            if (field.Localized)
            {
                ValidateLocalizedValue(field, property.Value, fieldPath, siteLocales, mode, errors);
            }
            else
            {
                ValidateValue(field, property.Value, fieldPath, siteLocales, mode, errors);
            }
        }

        foreach (ContentFieldDefinition field in schema)
        {
            if (!present.Contains(field.Key) && mode == ContentFieldsValidationMode.Publish && field.Required)
            {
                errors.Add(EntityError.Validation("Required field is missing.", $"{path}.{field.Key}"));
            }
        }
    }

    private static void ValidateLocalizedValue(
        ContentFieldDefinition field,
        JsonElement value,
        string path,
        IReadOnlyCollection<string> siteLocales,
        ContentFieldsValidationMode mode,
        List<EntityError> errors)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            errors.Add(EntityError.Validation("Localized field value must be an object keyed by locale.", path));
            return;
        }

        HashSet<string> knownLocales = new(siteLocales, StringComparer.Ordinal);
        HashSet<string> presentLocales = new(StringComparer.Ordinal);
        foreach (JsonProperty property in value.EnumerateObject())
        {
            string localePath = $"{path}.{property.Name}";
            if (!presentLocales.Add(property.Name))
            {
                errors.Add(EntityError.Validation("Localized values cannot contain duplicate locale keys.", localePath));
                continue;
            }

            if (!knownLocales.Contains(property.Name))
            {
                errors.Add(EntityError.Validation("Localized value uses a locale not configured for this site.", localePath));
                continue;
            }

            ValidateValue(field, property.Value, localePath, siteLocales, mode, errors);
        }

        if (mode == ContentFieldsValidationMode.Publish)
        {
            foreach (string locale in siteLocales)
            {
                if (!presentLocales.Contains(locale))
                {
                    errors.Add(EntityError.Validation("Localized field must contain every site locale when published.", $"{path}.{locale}"));
                }
            }
        }
    }

    private static void ValidateValue(
        ContentFieldDefinition field,
        JsonElement value,
        string path,
        IReadOnlyCollection<string> siteLocales,
        ContentFieldsValidationMode mode,
        List<EntityError> errors)
    {
        switch (field.Kind)
        {
            case ContentFieldKind.Text:
            case ContentFieldKind.LongText:
                ValidateString(value, path, field.MaxLength, errors);
                break;
            case ContentFieldKind.RichText:
                errors.AddRange(RichTextDocumentRules.Validate(value, path));
                break;
            case ContentFieldKind.Number:
                if (value.ValueKind != JsonValueKind.Number || !value.TryGetDouble(out double number) || !double.IsFinite(number))
                {
                    errors.Add(EntityError.Validation("Field value must be a number.", path));
                }
                break;
            case ContentFieldKind.Boolean:
                if (value.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                {
                    errors.Add(EntityError.Validation("Field value must be a boolean.", path));
                }
                break;
            case ContentFieldKind.Date:
                if (value.ValueKind != JsonValueKind.String ||
                    !DateOnly.TryParseExact(value.GetString(), DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                {
                    errors.Add(EntityError.Validation("Date field value must use YYYY-MM-DD format.", path));
                }
                break;
            case ContentFieldKind.Media:
                ValidateMedia(value, path, siteLocales, mode, errors);
                break;
            case ContentFieldKind.Link:
                ValidateLink(value, path, errors);
                break;
            case ContentFieldKind.Reference:
                if (value.ValueKind != JsonValueKind.String || !Guid.TryParse(value.GetString(), out _))
                {
                    errors.Add(EntityError.Validation("Reference field value must be a content item GUID.", path));
                }
                break;
            case ContentFieldKind.List:
                ValidateList(field, value, path, siteLocales, mode, errors);
                break;
            case ContentFieldKind.Group:
                ValidateFieldsObject(field.Fields, value, path, siteLocales, mode, errors);
                break;
            default:
                errors.Add(EntityError.Validation("Field kind is not supported.", path));
                break;
        }
    }

    private static void ValidateString(JsonElement value, string path, int? maximumLength, List<EntityError> errors)
    {
        if (value.ValueKind != JsonValueKind.String || value.GetString() is not string stringValue)
        {
            errors.Add(EntityError.Validation("Field value must be a string.", path));
            return;
        }

        if (maximumLength is int limit && stringValue.Length > limit)
        {
            errors.Add(EntityError.Validation($"Field value cannot exceed {limit} characters.", path));
        }
    }

    private static void ValidateList(
        ContentFieldDefinition field,
        JsonElement value,
        string path,
        IReadOnlyCollection<string> siteLocales,
        ContentFieldsValidationMode mode,
        List<EntityError> errors)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            errors.Add(EntityError.Validation("List field value must be an array.", path));
            return;
        }

        if (field.Item is null)
        {
            errors.Add(EntityError.Validation("List field schema requires an item definition.", path));
            return;
        }

        int index = 0;
        foreach (JsonElement item in value.EnumerateArray())
        {
            if (field.Item.Localized)
            {
                ValidateLocalizedValue(field.Item, item, $"{path}[{index}]", siteLocales, mode, errors);
            }
            else
            {
                ValidateValue(field.Item, item, $"{path}[{index}]", siteLocales, mode, errors);
            }
            index++;
        }
    }

    private static void ValidateMedia(
        JsonElement value,
        string path,
        IReadOnlyCollection<string> siteLocales,
        ContentFieldsValidationMode mode,
        List<EntityError> errors)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            errors.Add(EntityError.Validation("Media field value must be an object.", path));
            return;
        }

        if (!TryGetUniqueProperty(value, UrlProperty, path, errors, out JsonElement url) ||
            url.ValueKind != JsonValueKind.String || !ContentHrefRules.IsAllowed(url.GetString()))
        {
            errors.Add(EntityError.Validation("Media URL must use HTTPS or a single-leading-slash site-relative path.", $"{path}.{UrlProperty}"));
        }

        if (!TryGetUniqueProperty(value, AltProperty, path, errors, out JsonElement alt))
        {
            errors.Add(EntityError.Validation("Media alt text is required.", $"{path}.{AltProperty}"));
        }
        else if (alt.ValueKind == JsonValueKind.String)
        {
            // A plain string is the non-localized alternative for media alt text.
        }
        else if (alt.ValueKind == JsonValueKind.Object)
        {
            ValidateLocalizedString(alt, $"{path}.{AltProperty}", siteLocales, mode, errors);
        }
        else
        {
            errors.Add(EntityError.Validation("Media alt text must be a string or locale object.", $"{path}.{AltProperty}"));
        }

        ValidateOnlyProperties(value, [UrlProperty, AltProperty], path, errors);
    }

    private static void ValidateLink(JsonElement value, string path, List<EntityError> errors)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            errors.Add(EntityError.Validation("Link field value must be an object.", path));
            return;
        }

        if (!TryGetUniqueProperty(value, RichTextDocumentRules.HrefProperty, path, errors, out JsonElement href) ||
            href.ValueKind != JsonValueKind.String || !ContentHrefRules.IsAllowed(href.GetString()))
        {
            errors.Add(EntityError.Validation("Link href must use HTTPS or a single-leading-slash site-relative path.", $"{path}.{RichTextDocumentRules.HrefProperty}"));
        }

        if (!TryGetUniqueProperty(value, LabelProperty, path, errors, out JsonElement label) || label.ValueKind != JsonValueKind.String)
        {
            errors.Add(EntityError.Validation("Link label is required and must be a string.", $"{path}.{LabelProperty}"));
        }

        ValidateOnlyProperties(value, [RichTextDocumentRules.HrefProperty, LabelProperty], path, errors);
    }

    private static void ValidateLocalizedString(
        JsonElement value,
        string path,
        IReadOnlyCollection<string> siteLocales,
        ContentFieldsValidationMode mode,
        List<EntityError> errors)
    {
        HashSet<string> knownLocales = new(siteLocales, StringComparer.Ordinal);
        HashSet<string> presentLocales = new(StringComparer.Ordinal);
        foreach (JsonProperty property in value.EnumerateObject())
        {
            string localePath = $"{path}.{property.Name}";
            if (!presentLocales.Add(property.Name))
            {
                errors.Add(EntityError.Validation("Localized alt text cannot contain duplicate locale keys.", localePath));
                continue;
            }

            if (!knownLocales.Contains(property.Name))
                errors.Add(EntityError.Validation("Localized alt text uses a locale not configured for this site.", localePath));
            else if (property.Value.ValueKind != JsonValueKind.String)
                errors.Add(EntityError.Validation("Localized alt text values must be strings.", localePath));
        }

        if (mode == ContentFieldsValidationMode.Publish)
        {
            foreach (string locale in siteLocales)
            {
                if (!presentLocales.Contains(locale))
                {
                    errors.Add(EntityError.Validation("Localized alt text must contain every site locale when published.", $"{path}.{locale}"));
                }
            }
        }
    }

    private static bool TryGetUniqueProperty(
        JsonElement value,
        string propertyName,
        string path,
        List<EntityError> errors,
        out JsonElement propertyValue)
    {
        propertyValue = default;
        bool found = false;
        foreach (JsonProperty property in value.EnumerateObject())
        {
            if (property.Name != propertyName)
            {
                continue;
            }

            if (found)
            {
                errors.Add(EntityError.Validation("Object properties cannot be duplicated.", $"{path}.{propertyName}"));
                return false;
            }

            found = true;
            propertyValue = property.Value;
        }

        return found;
    }

    private static void ValidateOnlyProperties(
        JsonElement value,
        IReadOnlyCollection<string> allowed,
        string path,
        List<EntityError> errors)
    {
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (JsonProperty property in value.EnumerateObject())
        {
            if (!seen.Add(property.Name))
            {
                errors.Add(EntityError.Validation("Object properties cannot be duplicated.", $"{path}.{property.Name}"));
            }
            else if (!allowed.Contains(property.Name, StringComparer.Ordinal))
            {
                errors.Add(EntityError.Validation("Object contains an unsupported property.", $"{path}.{property.Name}"));
            }
        }
    }

}
