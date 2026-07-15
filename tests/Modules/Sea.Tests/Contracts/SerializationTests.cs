using System.Text.Json;
using FluentAssertions;
using Vitarish.Sea.Contracts.Errors;
using Vitarish.Sea.Contracts.Results;
using Vitarish.Sea.Contracts.Routes;
using Vitarish.Sea.Contracts.Responses;
using Vitarish.Sea.Contracts.Enums;

namespace Sea.Tests.Contracts;

public class SerializationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true };

    [Fact]
    public void SeaResult_success_round_trip()
    {
        var original = SeaResult<string>.Success("ok");
        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<SeaResult<string>>(json, JsonOptions);
        deserialized.Should().NotBeNull();
        deserialized!.Succeeded.Should().BeTrue();
        deserialized.Value.Should().Be("ok");
    }

    [Fact]
    public void SeaResult_failure_round_trip()
    {
        var error = new SeaError(SeaErrorCode.NotFound, "Not found");
        var original = SeaResult<string>.Failure(error);
        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<SeaResult<string>>(json, JsonOptions);
        deserialized.Should().NotBeNull();
        deserialized!.Succeeded.Should().BeFalse();
        deserialized.Error!.Code.Should().Be(SeaErrorCode.NotFound);
    }

    [Fact]
    public void SeaCommandResult_round_trip()
    {
        var id = Guid.NewGuid();
        var original = new SeaCommandResult(true, id);
        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<SeaCommandResult>(json, JsonOptions);
        deserialized.Should().NotBeNull();
        deserialized!.Succeeded.Should().BeTrue();
        deserialized.EntityId.Should().Be(id);
    }

    [Fact]
    public void SeaErrorCode_has_expected_values()
    {
        var values = Enum.GetValues<SeaErrorCode>();
        values.Should().Contain(SeaErrorCode.ValidationFailure);
        values.Should().Contain(SeaErrorCode.Unauthorized);
        values.Should().Contain(SeaErrorCode.Forbidden);
        values.Should().Contain(SeaErrorCode.NotFound);
        values.Should().Contain(SeaErrorCode.Conflict);
        values.Should().Contain(SeaErrorCode.Timeout);
        values.Should().Contain(SeaErrorCode.Cancellation);
        values.Should().Contain(SeaErrorCode.TransportFailure);
        values.Should().Contain(SeaErrorCode.DeserializationFailure);
        values.Should().Contain(SeaErrorCode.UnexpectedFailure);
    }

    [Fact]
    public void SeaApiRoutes_constants_are_correct()
    {
        SeaApiRoutes.Base.Should().Be("/api/modules/sea");
        SeaApiRoutes.Health.Should().Be("/health");
        SeaApiRoutes.Projects.Should().Be("/projects");
        SeaApiRoutes.Procedures.Should().Be("/procedures");
        SeaApiRoutes.Events.Should().Be("/events");
        SeaApiRoutes.Attachments.Should().Be("/attachments");
        SeaApiRoutes.Roles.Should().Be("/roles");
    }

    [Fact]
    public void Attachment_filename_round_trips_and_legacy_null_remains_valid()
    {
        var value = new AttachmentWriteDto(Guid.NewGuid(), "content", "application/pdf", null, 42, AttachmentKind.File, "report.pdf");

        var roundTrip = JsonSerializer.Deserialize<AttachmentWriteDto>(JsonSerializer.Serialize(value, JsonOptions), JsonOptions);
        var legacy = JsonSerializer.Deserialize<AttachmentWriteDto>("{\"eventId\":\"00000000-0000-0000-0000-000000000001\",\"content\":\"note\",\"contentType\":\"text/plain\",\"size\":4}", JsonOptions);

        roundTrip!.FileName.Should().Be("report.pdf");
        roundTrip.Kind.Should().Be(AttachmentKind.File);
        legacy!.FileName.Should().BeNull();
        legacy.Kind.Should().Be(AttachmentKind.Text);
    }
}
