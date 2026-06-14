using Elastic.Clients.Elasticsearch.QueryDsl;
using FluentAssertions;
using Vitarish.Search.Domain.Models;
using Vitarish.Search.Infrastructure.Services;
using Xunit;

namespace Vitarish.Search.Tests;

public sealed class SearchRetrievalTests
{
    [Fact]
    public void StrictExactQueryWorks()
    {
        Query query = SearchService.BuildQuery("exact title", new SearchOptions
        {
            RetrievalMode = SearchRetrievalMode.Strict
        }, "tenant-a", semanticRetrievalAvailable: false);

        BoolQuery boolQuery = query.Bool ?? throw new InvalidOperationException("Expected bool query.");

        boolQuery.Should.Should().Contain(q => q.MatchPhrase != null && q.MatchPhrase.Field != null && q.MatchPhrase.Field.ToString() == "title");
        SearchService.BuildAppliedStrategies(SearchRetrievalMode.Strict, semanticAvailable: false)
            .Should().Equal("exact", "lexical");
    }

    [Fact]
    public void BalancedTypoQueryWorks()
    {
        Query query = SearchService.BuildQuery("tyop", new SearchOptions
        {
            RetrievalMode = SearchRetrievalMode.Balanced
        }, "tenant-a", semanticRetrievalAvailable: false);

        BoolQuery boolQuery = query.Bool ?? throw new InvalidOperationException("Expected bool query.");

        boolQuery.Should.Should().Contain(q => q.MultiMatch != null && q.MultiMatch.Fuzziness != null);
        SearchService.BuildAppliedStrategies(SearchRetrievalMode.Balanced, semanticAvailable: false)
            .Should().Equal("exact", "lexical", "fuzzy");
    }

    [Fact]
    public void BroadSemanticQueryWorksWhenInferenceIsConfigured()
    {
        Query query = SearchService.BuildQuery("conceptual question", new SearchOptions
        {
            RetrievalMode = SearchRetrievalMode.Broad
        }, "tenant-a", semanticRetrievalAvailable: true);

        BoolQuery boolQuery = query.Bool ?? throw new InvalidOperationException("Expected bool query.");

        boolQuery.Should.Should().Contain(q => q.Match != null && q.Match.Field != null && q.Match.Field.ToString() == "semanticText");
        SearchService.BuildAppliedStrategies(SearchRetrievalMode.Broad, semanticAvailable: true)
            .Should().Equal("exact", "lexical", "fuzzy", "semantic");
    }

    [Fact]
    public void BroadFallsBackSafelyWhenInferenceIsMissing()
    {
        Query query = SearchService.BuildQuery("conceptual question", new SearchOptions
        {
            RetrievalMode = SearchRetrievalMode.Broad
        }, "tenant-a", semanticRetrievalAvailable: false);

        BoolQuery boolQuery = query.Bool ?? throw new InvalidOperationException("Expected bool query.");

        boolQuery.Should.Should().NotContain(q => q.Match != null && q.Match.Field != null && q.Match.Field.ToString() == "semanticText");
        SearchService.BuildAppliedStrategies(SearchRetrievalMode.Broad, semanticAvailable: false)
            .Should().Equal("exact", "lexical", "fuzzy");
    }
}
