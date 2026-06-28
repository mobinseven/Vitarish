using System.Text.Json;
using FluentAssertions;
using Vitarish.Search.Contracts.Errors;
using Vitarish.Search.Contracts.Routes;

namespace Search.Tests.Contracts;

public class SerializationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true };

    [Fact]
    public void SearchErrorCode_has_expected_values()
    {
        var values = Enum.GetValues<SearchErrorCode>();
        values.Should().Contain(SearchErrorCode.Validation);
        values.Should().Contain(SearchErrorCode.Unauthorized);
        values.Should().Contain(SearchErrorCode.Forbidden);
        values.Should().Contain(SearchErrorCode.Timeout);
        values.Should().Contain(SearchErrorCode.Canceled);
        values.Should().Contain(SearchErrorCode.Transport);
        values.Should().Contain(SearchErrorCode.Deserialization);
        values.Should().Contain(SearchErrorCode.Unexpected);
    }

    [Fact]
    public void SearchApiRoutes_constants_are_correct()
    {
        SearchApiRoutes.Base.Should().Be("/api/modules/search");
        SearchApiRoutes.IndexStatus.Should().Be("/index-status");
        SearchApiRoutes.RebuildIndex.Should().Be("/index/rebuild");
    }
}
