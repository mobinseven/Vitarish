using Microsoft.Kiota.Abstractions.Serialization;
using Vitarish.Prescription.Domain.Entities;
using Vitarish.Prescription.Domain.Enums;

namespace Vitarish.Prescription.Infrastructure.Providers.Tamin;

/// <summary>
/// Builds Kiota-serializable Tamin edit item payloads from prescription items.
/// </summary>
internal static class TaminEditedPrescriptionItemMapper
{
    public static IReadOnlyList<object> ToEditedItems(IEnumerable<PrescriptionItem> items) => items
        .Select(ToEditedItem)
        .Cast<object>()
        .ToList();

    private static UntypedObject ToEditedItem(PrescriptionItem item)
    {
        Dictionary<string, UntypedNode> properties = new(StringComparer.Ordinal)
        {
            [CodePropertyName(item.Category)] = new UntypedString(item.ReferenceCode ?? string.Empty),
            ["quantity"] = new UntypedInteger(item.Quantity)
        };

        return new UntypedObject(properties);
    }

    private static string CodePropertyName(PrescriptionItemCategory category) => category == PrescriptionItemCategory.Drug
        ? "drug_code"
        : "service_code";
}
