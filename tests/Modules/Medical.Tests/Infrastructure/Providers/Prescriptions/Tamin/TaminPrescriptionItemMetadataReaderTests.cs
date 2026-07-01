using FluentAssertions;
using Vitarish.Medical.Domain.Entities.Prescriptions;
using Vitarish.Medical.Domain.Enums.Prescriptions;
using Vitarish.Medical.Infrastructure.Providers.Prescriptions.Tamin;

namespace Medical.Tests.Infrastructure.Providers.Prescriptions.Tamin;

public sealed class TaminPrescriptionItemMetadataReaderTests
{
    [Fact]
    public void ReadString_returns_first_matching_string_metadata_value()
    {
        PrescriptionItem item = CreateItem("""{ "target_specialty": "cardiology", "reason": "follow-up" }""");

        string? result = TaminPrescriptionItemMetadataReader.ReadString(item, "targetSpecialty", "target_specialty");

        result.Should().Be("cardiology");
    }

    [Theory]
    [InlineData("{ \"sessionCount\": 12 }", 12)]
    [InlineData("{ \"session_count\": \"8\" }", 8)]
    [InlineData("{ \"sessionCount\": 0 }", null)]
    [InlineData("{ \"sessionCount\": -1 }", null)]
    [InlineData("{ \"sessionCount\": \"not-number\" }", null)]
    public void ReadPositiveInt_accepts_only_positive_numeric_metadata_values(string metadataJson, int? expected)
    {
        PrescriptionItem item = CreateItem(metadataJson);

        int? result = TaminPrescriptionItemMetadataReader.ReadPositiveInt(item, "sessionCount", "session_count");

        result.Should().Be(expected);
    }

    [Fact]
    public void Readers_return_null_for_missing_item_or_invalid_json()
    {
        TaminPrescriptionItemMetadataReader.ReadString(null, "reason").Should().BeNull();
        TaminPrescriptionItemMetadataReader.ReadPositiveInt(null, "sessionCount").Should().BeNull();

        PrescriptionItem invalid = CreateItem("{ invalid json");

        TaminPrescriptionItemMetadataReader.ReadString(invalid, "reason").Should().BeNull();
        TaminPrescriptionItemMetadataReader.ReadPositiveInt(invalid, "sessionCount").Should().BeNull();
    }

    private static PrescriptionItem CreateItem(string metadataJson) => new()
    {
        Id = Guid.NewGuid(),
        PrescriptionRecordId = Guid.NewGuid(),
        Category = PrescriptionItemCategory.Physiotherapy,
        ReferenceCode = "R1",
        DisplayName = "Reference item",
        Quantity = 1,
        MetadataJson = metadataJson
    };
}
