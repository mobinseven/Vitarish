namespace Vitarish.Sea.Infrastructure.Helpers;

using System;
using System.Text.Json;

internal static class OverviewMetadataParser
{
    public static (string? Overview, DateTimeOffset? UpdatedAt) Parse(string metadata)
    {
        if(string.IsNullOrWhiteSpace(metadata))
        {
            return (null, null);
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(metadata);
            if(document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return (null, null);
            }

            string? overview = null;
            DateTimeOffset? updatedAt = null;

            if(document.RootElement.TryGetProperty("overview", out JsonElement overviewElement) &&
                overviewElement.ValueKind == JsonValueKind.String)
            {
                overview = overviewElement.GetString();
            }

            if(document.RootElement.TryGetProperty("overviewUpdatedAt", out JsonElement updatedAtElement))
            {
                if(updatedAtElement.ValueKind == JsonValueKind.String &&
                    updatedAtElement.TryGetDateTimeOffset(out DateTimeOffset parsed))
                {
                    updatedAt = parsed;
                }
                else if(updatedAtElement.ValueKind == JsonValueKind.Number &&
                    updatedAtElement.TryGetInt64(out long epoch))
                {
                    try
                    {
                        updatedAt = DateTimeOffset.FromUnixTimeMilliseconds(epoch);
                    }
                    catch(ArgumentOutOfRangeException)
                    {
                    }
                }
            }

            overview = string.IsNullOrWhiteSpace(overview) ? null : overview.Trim();
            return (overview, updatedAt);
        }
        catch(JsonException)
        {
            return (null, null);
        }
    }
}
