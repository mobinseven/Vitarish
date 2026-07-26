using System.Text.Json;
using FluentAssertions;
using Vitarish.Medical.Contracts.Errors;
using Vitarish.Medical.Contracts.Requests;
using Vitarish.Medical.Contracts.Results;
using Vitarish.Medical.Contracts.Routes;

namespace Medical.Tests.Contracts;

public class SerializationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true };

    [Fact]
    public void MedicalResult_success_round_trip()
    {
        var original = MedicalResult<string>.Success("ok");
        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<MedicalResult<string>>(json, JsonOptions);
        deserialized.Should().NotBeNull();
        deserialized!.IsSuccess.Should().BeTrue();
        deserialized.Value.Should().Be("ok");
    }

    [Fact]
    public void MedicalResult_failure_round_trip()
    {
        var error = new MedicalError(MedicalErrorCode.NotFound, "Not found");
        var original = MedicalResult<string>.Failed(error);
        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<MedicalResult<string>>(json, JsonOptions);
        deserialized.Should().NotBeNull();
        deserialized!.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void MedicalErrorCode_has_expected_values()
    {
        var values = Enum.GetValues<MedicalErrorCode>();
        values.Should().NotBeEmpty();
        values.Should().Contain(MedicalErrorCode.ValidationFailure);
        values.Should().Contain(MedicalErrorCode.Unauthorized);
        values.Should().Contain(MedicalErrorCode.NotFound);
        values.Should().Contain(MedicalErrorCode.ProviderUnavailable);
        values.Should().Contain(MedicalErrorCode.Timeout);
        values.Should().Contain(MedicalErrorCode.UnexpectedFailure);
    }

    [Fact]
    public void MedicalApiRoutes_constants_are_correct()
    {
        MedicalApiRoutes.Base.Should().Be("/api/modules/medical");
    }

    [Fact]
    public void Prescription_reference_code_search_contract_requires_provider_and_allows_cross_category_search()
    {
        var request = new SearchPrescriptionReferenceCodesRequest("tamin", "acetaminophen");

        string json = JsonSerializer.Serialize(request, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<SearchPrescriptionReferenceCodesRequest>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.ProviderKey.Should().Be("tamin");
        deserialized.Category.Should().BeNull();
        deserialized.TextOrCode.Should().Be("acetaminophen");
    }
}
