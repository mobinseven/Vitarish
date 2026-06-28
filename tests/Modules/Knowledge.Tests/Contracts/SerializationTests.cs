using System.Text.Json;
using FluentAssertions;
using Vitarish.Knowledge.Contracts.Errors;
using Vitarish.Knowledge.Contracts.Results;

namespace Knowledge.Tests.Contracts;

public class SerializationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true };

    [Fact]
    public void KnowledgeResult_success_round_trip()
    {
        var original = KnowledgeResult<string>.Success("ok");
        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<KnowledgeResult<string>>(json, JsonOptions);
        deserialized.Should().NotBeNull();
        deserialized!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void KnowledgeResult_failure_round_trip()
    {
        var error = new KnowledgeContractError(KnowledgeErrorCode.NotFound, "Not found");
        var original = KnowledgeResult<string>.Failed(error);
        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<KnowledgeResult<string>>(json, JsonOptions);
        deserialized.Should().NotBeNull();
        deserialized!.Succeeded.Should().BeFalse();
    }
}
