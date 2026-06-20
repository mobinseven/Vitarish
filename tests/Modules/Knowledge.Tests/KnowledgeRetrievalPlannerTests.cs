using FluentAssertions;
using Vitarish.Intelligence.Application.Abstractions;
using Vitarish.Intelligence.Domain.Models;
using Vitarish.Knowledge.Application.Contracts;
using Vitarish.Knowledge.Application.Services.RetrievalPlanning;
using Xunit;

namespace Vitarish.Knowledge.Tests;

public sealed class KnowledgeRetrievalPlannerTests
{
    [Fact]
    public async Task PlanAsync_PreservesOriginalQuestionAndAddsCleanedQueryWithoutAiForShortQuestions()
    {
        KnowledgeRetrievalPlanner planner = new(new RecordingResponder("{}"));

        KnowledgeRetrievalPlan plan = await planner.PlanAsync("  Why???   does   task A fail??  ", CancellationToken.None);

        _ = plan.OriginalQuestion.Should().Be("  Why???   does   task A fail??  ");
        _ = plan.CleanedQuery.Should().Be("Why? does task A fail?");
        _ = plan.SearchQueries.Should().Equal("Why? does task A fail?");
    }

    [Fact]
    public async Task PlanAsync_AddsFocusedAiQueriesAfterOriginalIntentAndCapsDuplicates()
    {
        string longQuestion = string.Join(' ', Enumerable.Repeat("Contoso Sync fails when import validation rejects the customer mapping example but only after the retry constraint runs.", 3));
        string aiJson = """
        {
          "searchQueries": [
            "Contoso Sync import validation customer mapping fails",
            "retry constraint rejects customer mapping example",
            "Contoso Sync import validation customer mapping fails",
            "unsupported extra query",
            "another unsupported extra query"
          ],
          "preservedTerms": ["Contoso Sync", "customer mapping"],
          "expandedTerms": ["import validation"],
          "droppedTerms": [],
          "warnings": []
        }
        """;
        KnowledgeRetrievalPlanner planner = new(new RecordingResponder(aiJson));

        KnowledgeRetrievalPlan plan = await planner.PlanAsync(longQuestion, CancellationToken.None);

        _ = plan.SearchQueries.Should().HaveCount(5);
        _ = plan.SearchQueries[0].Should().Be(longQuestion);
        _ = plan.SearchQueries.Should().Contain("Contoso Sync import validation customer mapping fails");
        _ = plan.SearchQueries.Should().Contain("retry constraint rejects customer mapping example");
        _ = plan.SearchQueries.Distinct(StringComparer.OrdinalIgnoreCase).Should().HaveSameCount(plan.SearchQueries);
        _ = plan.PreservedTerms.Should().Equal("Contoso Sync", "customer mapping");
        _ = plan.ExpandedTerms.Should().Equal("import validation");
    }

    private sealed class RecordingResponder(string json) : IResponder
    {
        public Task<IReadOnlyList<ResponderOutput>> GenerateAsync(IReadOnlyList<ResponderInput> input, ResponderOptions options, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<ResponderOutput>>([ResponderOutput.TextOutput(json)]);

        public IAsyncEnumerable<ResponderOutput> StreamAsync(IReadOnlyList<ResponderInput> input, ResponderOptions options, CancellationToken ct = default) => throw new NotSupportedException();
    }
}
