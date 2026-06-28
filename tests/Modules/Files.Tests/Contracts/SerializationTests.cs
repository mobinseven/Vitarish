using System.Text.Json;
using FluentAssertions;
using Vitarish.Files.Contracts.Errors;
using Vitarish.Files.Contracts.Results;
using Vitarish.Files.Contracts.Routes;

namespace Files.Tests.Contracts;

public class SerializationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true };

    [Fact]
    public void FilesResult_success_round_trip()
    {
        var original = FilesResult<string>.Success("ok");
        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<FilesResult<string>>(json, JsonOptions);
        deserialized.Should().NotBeNull();
        deserialized!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void FilesErrorCode_has_expected_values()
    {
        var values = Enum.GetValues<FilesErrorCode>();
        values.Should().Contain(FilesErrorCode.Validation);
        values.Should().Contain(FilesErrorCode.Unauthorized);
        values.Should().Contain(FilesErrorCode.Forbidden);
        values.Should().Contain(FilesErrorCode.NotFound);
        values.Should().Contain(FilesErrorCode.Conflict);
        values.Should().Contain(FilesErrorCode.Timeout);
        values.Should().Contain(FilesErrorCode.Canceled);
        values.Should().Contain(FilesErrorCode.Transport);
        values.Should().Contain(FilesErrorCode.Deserialization);
        values.Should().Contain(FilesErrorCode.Unexpected);
    }

    [Fact]
    public void FilesApiRoutes_constants_are_correct()
    {
        FilesApiRoutes.Base.Should().Be("/api/modules/files");
        FilesApiRoutes.ById.Should().Be("/{fileId:guid}");
        FilesApiRoutes.ContentById.Should().Be("/{fileId:guid}/content");
        FilesApiRoutes.Upload.Should().Be("/upload");
    }
}
