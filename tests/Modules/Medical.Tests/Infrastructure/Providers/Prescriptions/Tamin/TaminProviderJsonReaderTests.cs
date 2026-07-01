using System.Text.Json;
using FluentAssertions;
using Vitarish.Medical.Infrastructure.Providers.Prescriptions.Tamin;

namespace Medical.Tests.Infrastructure.Providers.Prescriptions.Tamin;

public sealed class TaminProviderJsonReaderTests
{
    [Fact]
    public void ToReferencePage_maps_root_array_items_and_preserves_raw_json()
    {
        using JsonDocument document = JsonDocument.Parse("""
        [
          { "drug_code": "D001", "drug_name": "Drug one", "is_active": true },
          { "service_code": "S001", "service_name": "Service one", "status": "inactive" }
        ]
        """);

        TaminReferencePageResult result = TaminProviderJsonReader.ToReferencePage(
            TaminProviderOperationNames.Drug,
            document.RootElement,
            fallbackPage: 2,
            fallbackPageSize: 10);

        result.Page.Should().Be(2);
        result.PageSize.Should().Be(10);
        result.Items.Should().HaveCount(2);
        result.Items[0].Code.Should().Be("D001");
        result.Items[0].DisplayName.Should().Be("Drug one");
        result.Items[0].IsActive.Should().BeTrue();
        result.Items[1].Code.Should().Be("S001");
        result.Items[1].DisplayName.Should().Be("Service one");
        result.Items[1].IsActive.Should().BeFalse();
        result.RawJson.Should().Contain("D001");
    }

    [Fact]
    public void ToReferencePage_maps_nested_data_items_and_paging_metadata_from_nested_envelope()
    {
        using JsonDocument document = JsonDocument.Parse("""
        {
          "data": {
            "items": [
              { "srvCode": "LAB1", "srvName": "Lab service", "serviceGroup": "lab" }
            ],
            "page": 3,
            "pageSize": 25,
            "totalCount": 80,
            "dataVersion": "v-14050311"
          }
        }
        """);

        TaminReferencePageResult result = TaminProviderJsonReader.ToReferencePage(
            TaminProviderOperationNames.Service,
            document.RootElement,
            fallbackPage: 1,
            fallbackPageSize: 20);

        result.Page.Should().Be(3);
        result.PageSize.Should().Be(25);
        result.TotalCount.Should().Be(80);
        result.HasNextPage.Should().BeTrue();
        result.DataVersion.Should().Be("v-14050311");
        result.Items.Should().ContainSingle(item =>
            item.Code == "LAB1" &&
            item.DisplayName == "Lab service" &&
            item.Category == "lab" &&
            item.ProviderKind == TaminProviderOperationNames.Service);
    }

    [Fact]
    public void ToReferencePage_filters_empty_items_and_marks_short_unknown_page_as_terminal()
    {
        using JsonDocument document = JsonDocument.Parse("""
        {
          "items": [
            { "ignored": true },
            { "code": "A1", "name": "Active item" }
          ],
          "page": 1,
          "page_size": 10,
          "version": "v1"
        }
        """);

        TaminReferencePageResult result = TaminProviderJsonReader.ToReferencePage(
            TaminProviderOperationNames.AllServices,
            document.RootElement,
            fallbackPage: null,
            fallbackPageSize: null);

        result.Items.Should().ContainSingle();
        result.HasNextPage.Should().BeFalse();
        result.DataVersion.Should().Be("v1");
    }
}
