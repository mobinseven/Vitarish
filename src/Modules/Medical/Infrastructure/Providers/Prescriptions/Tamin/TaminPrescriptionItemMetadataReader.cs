using System.Globalization;
using System.Text.Json;
using Vitarish.Medical.Domain.Entities.Prescriptions;

namespace Vitarish.Medical.Infrastructure.Providers.Prescriptions.Tamin;

internal static class TaminPrescriptionItemMetadataReader
{
    public static string? ReadString(PrescriptionItem? item, params string[] propertyNames)
    {
        if (item is null || string.IsNullOrWhiteSpace(item.MetadataJson))
        {
            return null;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(item.MetadataJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            foreach (string propertyName in propertyNames)
            {
                if (document.RootElement.TryGetProperty(propertyName, out JsonElement value))
                {
                    string? text = value.ValueKind switch
                    {
                        JsonValueKind.String => value.GetString(),
                        JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => value.ToString(),
                        _ => null
                    };

                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        return text;
                    }
                }
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }

    public static int? ReadPositiveInt(PrescriptionItem? item, params string[] propertyNames)
    {
        string? value = ReadString(item, propertyNames);
        return int.TryParse(value, CultureInfo.InvariantCulture, out int parsed) && parsed > 0 ? parsed : null;
    }
}
