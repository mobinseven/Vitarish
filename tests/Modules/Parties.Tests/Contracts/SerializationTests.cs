using System.Text.Json;
using FluentAssertions;
using Vitarish.Parties.Contracts.Errors;
using Vitarish.Parties.Contracts.Results;
using Vitarish.Parties.Contracts.Routes;

namespace Parties.Tests.Contracts;

public class SerializationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void PartiesResult_success_round_trip()
    {
        var original = PartiesResult<string>.Success("ok");

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<PartiesResult<string>>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.IsSuccess.Should().BeTrue();
        deserialized.Value.Should().Be("ok");
    }

    [Fact]
    public void PartiesResult_failed_round_trip()
    {
        var error = new PartiesError(PartiesErrorCode.NotFound, "Not found");
        var original = PartiesResult<string>.Failed(error);

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<PartiesResult<string>>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.IsSuccess.Should().BeFalse();
        deserialized.Error.Should().NotBeNull();
        deserialized.Error!.Code.Should().Be(PartiesErrorCode.NotFound);
    }

    [Fact]
    public void PartiesError_round_trip()
    {
        var original = new PartiesError(PartiesErrorCode.Validation, "Invalid", new Dictionary<string, string[]> { ["name"] = ["Required"] });

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<PartiesError>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.Code.Should().Be(PartiesErrorCode.Validation);
        deserialized.ValidationErrors.Should().ContainKey("name");
    }

    [Fact]
    public void PartiesErrorCode_has_expected_values()
    {
        var values = Enum.GetValues<PartiesErrorCode>();

        values.Should().Contain(PartiesErrorCode.Validation);
        values.Should().Contain(PartiesErrorCode.Unauthorized);
        values.Should().Contain(PartiesErrorCode.Forbidden);
        values.Should().Contain(PartiesErrorCode.NotFound);
        values.Should().Contain(PartiesErrorCode.Conflict);
        values.Should().Contain(PartiesErrorCode.Unexpected);
    }

    [Fact]
    public void PartiesApiRoutes_constants_are_correct()
    {
        PartiesApiRoutes.Base.Should().Be("/api/modules/parties");
        PartiesApiRoutes.UserTenants.Should().Be("/user-tenants");
        PartiesApiRoutes.Owners.Should().Be("/owners");
        PartiesApiRoutes.OwnerById.Should().Be("/owners/{ownerId:guid}");
        PartiesApiRoutes.Tenants.Should().Be("/tenants");
        PartiesApiRoutes.TenantById.Should().Be("/tenants/{tenantId:guid}");
        PartiesApiRoutes.TenantsByOwner.Should().Be("/owners/{ownerId:guid}/tenants");
        PartiesApiRoutes.TenantIndicators.Should().Be("/tenant-indicators");
        PartiesApiRoutes.TenantIndicatorsByTenant.Should().Be("/tenants/{tenantId:guid}/indicators");
        PartiesApiRoutes.TenantIndicatorById.Should().Be("/tenant-indicators/{tenantIndicatorId:guid}");
        PartiesApiRoutes.SetupTenant.Should().Be("/tenant-setup");
    }
}
