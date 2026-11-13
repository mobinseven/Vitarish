using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http.HttpResults;
using Vitarish.Intelligence.Api;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Intelligence.Application.GuidedSurfaces;
using Vitarish.Intelligence.Contracts;

namespace Vitarish.Intelligence.Tests.GuidedSurfaces;

public sealed class GuidedSurfaceBlockResponseMappingTests
{
    [Fact]
    public void Question_block_maps_to_a_question_response_with_only_that_member()
    {
        GuidedSurfaceBlock block = GuidedSurfaceBlock.ModelQuestion(
            new AgentInteractiveQuestion("Which?", [new("a", "Alpha"), new("b", "Beta")], 2));

        GuidedSurfaceBlockResponse response = IntelligenceAgentConversationResultMapper.ToResponse(block);

        response.Kind.Should().Be(GuidedSurfaceBlockKindResponse.Question);
        response.NextAction.Should().BeNull();
        response.ActionGroup.Should().BeNull();
        response.Question.Should().NotBeNull();
        response.Question!.Prompt.Should().Be("Which?");
        response.Question.MaxSelections.Should().Be(2);
        response.Question.Options.Should().Equal(new GuidedSurfaceOptionResponse("a", "Alpha"), new GuidedSurfaceOptionResponse("b", "Beta"));
    }

    [Fact]
    public void Next_action_block_maps_to_a_next_action_response_with_only_that_member()
    {
        GuidedSurfaceBlockResponse response = IntelligenceAgentConversationResultMapper.ToResponse(GuidedSurfaceBlock.NextActionKey("StaffUsers"));

        response.Kind.Should().Be(GuidedSurfaceBlockKindResponse.NextAction);
        response.NextAction.Should().Be("StaffUsers");
        response.Question.Should().BeNull();
        response.ActionGroup.Should().BeNull();
    }

    [Fact]
    public void Action_group_block_maps_to_an_action_group_response_with_only_that_member()
    {
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();

        GuidedSurfaceBlockResponse response = IntelligenceAgentConversationResultMapper.ToResponse(
            GuidedSurfaceBlock.PreparedActions("run-9", [first, second]));

        response.Kind.Should().Be(GuidedSurfaceBlockKindResponse.ActionGroup);
        response.ActionGroup!.AgentRunId.Should().Be("run-9");
        response.ActionGroup.ActionIds.Should().Equal(first, second);
        response.Question.Should().BeNull();
        response.NextAction.Should().BeNull();
    }

    [Fact]
    public void Every_block_kind_has_a_matching_response_kind()
    {
        Enum.GetNames<GuidedSurfaceBlockKindResponse>().Should().Equal(Enum.GetNames<GuidedSurfaceBlockKind>());
    }

    [Fact]
    public void Succeeded_result_carries_the_mapped_blocks_in_order()
    {
        AgentConversationEndpointResult result = Succeeded([
            GuidedSurfaceBlock.PreparedActions("run-1", [Guid.NewGuid()]),
            GuidedSurfaceBlock.NextActionKey("ClinicProfile")]);

        RunAgentConversationResponse response = Response(result);

        response.Blocks.Should().NotBeNull();
        response.Blocks!.Select(block => block.Kind).Should().Equal(
            GuidedSurfaceBlockKindResponse.ActionGroup, GuidedSurfaceBlockKindResponse.NextAction);
    }

    [Fact]
    public void Succeeded_result_without_blocks_has_null_blocks()
    {
        Response(Succeeded(null)).Blocks.Should().BeNull();
    }

    [Fact]
    public void Succeeded_result_with_empty_blocks_has_null_blocks()
    {
        Response(Succeeded([])).Blocks.Should().BeNull();
    }

    private static AgentConversationEndpointResult Succeeded(IReadOnlyList<GuidedSurfaceBlock>? blocks) => new(
        AgentConversationOutcome.Succeeded,
        "run-1",
        Guid.Parse("018f47c2-a6b7-7000-8000-000000000001"),
        JsonDocument.Parse("{}").RootElement.Clone(),
        blocks);

    private static RunAgentConversationResponse Response(AgentConversationEndpointResult result) =>
        IntelligenceAgentConversationResultMapper.ToHttpResult(result)
            .Should().BeOfType<Ok<RunAgentConversationResponse>>().Subject.Value!;
}
