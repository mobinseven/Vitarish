using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Vitarish.Contents.Contracts.Models;

namespace Vitarish.Contents.Api;

internal static partial class StructuredContentsApi
{
    private static IResult? ValidateSite(SiteRequest request)
    {
        Dictionary<string, string[]> errors = [];
        Required(errors, nameof(request.Key), request.Key, 256);
        Required(errors, nameof(request.Name), request.Name, 256);
        Required(errors, nameof(request.DefaultLocale), request.DefaultLocale, 32);
        if (request.Locales is null || request.Locales.Length == 0 || request.Locales.Any(locale => string.IsNullOrWhiteSpace(locale) || locale.Length > 32))
            errors[nameof(request.Locales)] = ["At least one locale of at most 32 characters is required."];
        return Problem(errors);
    }

    private static IResult? ValidateType(ContentTypeRequest request)
    {
        Dictionary<string, string[]> errors = [];
        Required(errors, nameof(request.Key), request.Key, 256);
        Required(errors, nameof(request.Name), request.Name, 256);
        if (request.Description?.Length > 4000) errors[nameof(request.Description)] = ["Description is too long."];
        ValidateDefinitions(errors, request.Fields);
        return Problem(errors);
    }

    private static IResult? ValidateSchema(ContentSchemaRequest request)
    {
        Dictionary<string, string[]> errors = [];
        ValidateDefinitions(errors, request.Fields);
        return Problem(errors);
    }

    private static void ValidateDefinitions(Dictionary<string, string[]> errors, IReadOnlyList<ContentFieldDefinition>? fields)
    {
        if (fields is null)
        {
            errors[nameof(ContentSchemaRequest.Fields)] = ["Each field requires a key and label of at most 256 characters."];
            return;
        }
        Stack<ContentFieldDefinition?> pending = new(fields);
        while (pending.TryPop(out ContentFieldDefinition? field))
        {
            if (field is null || string.IsNullOrWhiteSpace(field.Key) || field.Key.Length > 256 || string.IsNullOrWhiteSpace(field.Label) || field.Label.Length > 256 || !Enum.IsDefined(field.Kind))
            {
                errors[nameof(ContentSchemaRequest.Fields)] = ["Each field requires a valid kind, key and label of at most 256 characters."];
                return;
            }
            if (field.Item is not null) pending.Push(field.Item);
            if (field.Fields is not null)
                foreach (ContentFieldDefinition child in field.Fields) pending.Push(child);
        }
    }

    private static IResult? ValidateEntry(string? slug, string? path, string? title, string? excerpt, JsonElement fields)
    {
        Dictionary<string, string[]> errors = [];
        Required(errors, nameof(EntryRequest.Slug), slug, 256);
        Required(errors, nameof(EntryRequest.CanonicalPath), path, 512);
        Required(errors, nameof(EntryRequest.Title), title, 512);
        if (excerpt?.Length > 4000) errors[nameof(EntryRequest.Excerpt)] = ["Excerpt is too long."];
        if (fields.ValueKind != JsonValueKind.Object) errors[nameof(EntryRequest.Fields)] = ["Fields must be an object."];
        return Problem(errors);
    }

    private static void Required(Dictionary<string, string[]> errors, string name, string? value, int limit)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > limit) errors[name] = [$"{name} is required and must not exceed {limit} characters."];
    }

    private static IResult? Problem(Dictionary<string, string[]> errors) => errors.Count == 0 ? null : Results.ValidationProblem(errors);
}
