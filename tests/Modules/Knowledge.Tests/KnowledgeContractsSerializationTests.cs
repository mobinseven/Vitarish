using System.Text.Json;
using FluentAssertions;
using Vitarish.Knowledge.Contracts.Enums;
using Vitarish.Knowledge.Contracts.Errors;
using Vitarish.Knowledge.Contracts.Models;
using Vitarish.Knowledge.Contracts.Requests;
using Vitarish.Knowledge.Contracts.Responses;
using Vitarish.Knowledge.Contracts.Results;
using Vitarish.Knowledge.Contracts.Routes;
using Xunit;

namespace Vitarish.Knowledge.Tests;

public sealed class KnowledgeContractsSerializationTests
{
    [Fact]
    public void Routes_UseProtectedKnowledgeApiBase()
    {
        KnowledgeApiRoutes.Base.Should().Be("/api/modules/knowledge");
        KnowledgeApiRoutes.OwnerWorkspace.Should().StartWith("/");
        KnowledgeApiRoutes.DiagnosticsChunks.Should().Contain("diagnostics");
    }

    [Fact]
    public void PublicEnums_SerializeByNumberWithoutBackendReferences()
    {
        JsonSerializer.Serialize(KnowledgeSourceInputFormat.TechQaJson).Should().Be("2");
        JsonSerializer.Serialize(KnowledgeEvaluationScope.RandomSample).Should().Be("1");
        JsonSerializer.Serialize(KnowledgeFailureStage.Retrieval).Should().NotBeNullOrWhiteSpace();
        JsonSerializer.Serialize(KnowledgeRetrievalMode.Broad).Should().Be("1");
    }

    [Fact]
    public void RequestResponseResultAndDiagnosticsContracts_RoundTripAsInertJsonShapes()
    {
        var answer = new KnowledgeAnswerModel(
            "answer",
            true,
            [new KnowledgeCitation(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "title", "quote")],
            null,
            [new KnowledgeRetrievalHit(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "text", "title", 0.75)]);
        var command = new KnowledgeResult<ImportKnowledgeSourceResponse>(
            new ImportKnowledgeSourceResponse(true, "Imported", "1 source"),
            null);
        var diagnostics = new KnowledgePipelineDiagnosticsModel(
            new(true, "ok", new Dictionary<string, object?>()),
            new(true, "ok", new Dictionary<string, object?>()),
            new(true, "ok", new Dictionary<string, object?>()),
            new(true, "ok", new Dictionary<string, object?>()),
            new(true, "ok", new Dictionary<string, object?>()),
            new(true, "ok", new Dictionary<string, object?>()),
            new(true, "ok", new Dictionary<string, object?>()),
            new(true, "ok", new Dictionary<string, object?>()),
            new KnowledgeAskDebugModel(Guid.NewGuid(), "tenant", "question", [], [], answer, KnowledgeFailureStage.None, null));
        var request = new AskKnowledgeRequest(Guid.NewGuid(), "question", RetrievalMode: KnowledgeRetrievalMode.Balanced);
        var error = new KnowledgeContractError(KnowledgeErrorCode.UnsupportedFileType, "Unsupported.");

        RoundTrip(request).Question.Should().Be("question");
        RoundTrip(answer).RetrievedContext.Should().HaveCount(1);
        RoundTrip(command).Value!.Summary.Should().Be("1 source");
        RoundTrip(diagnostics).DebugAsk!.Answer.Answer.Should().Be("answer");
        RoundTrip(error).Code.Should().Be(KnowledgeErrorCode.UnsupportedFileType);
    }

    private static T RoundTrip<T>(T value) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value))!;
}
