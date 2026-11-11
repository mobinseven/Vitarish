using FluentAssertions;
using Vitarish.Clinic.Application.Assistant;
using Vitarish.Clinic.Application.Setup;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Intelligence.Application.GuidedSurfaces;
using Xunit;

namespace Vitarish.Clinic.Application.Tests.Assistant;

public sealed class AssistantGuidedSurfaceTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2030-01-01T10:00:00Z");

    [Fact]
    public void Next_action_keys_are_exactly_the_setup_goal_names()
    {
        AssistantNextActions.Keys.Should().BeEquivalentTo(Enum.GetNames<ClinicSetupGoal>());
    }

    [Fact]
    public void A_reply_with_nothing_to_show_has_no_blocks()
    {
        AssistantGuidedSurface.Blocks(Result([])).Should().BeEmpty();
    }

    [Fact]
    public void Null_result_is_rejected()
    {
        FluentActions.Invoking(() => AssistantGuidedSurface.Blocks(null!)).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Prepared_actions_become_one_action_group_with_the_run_id_and_ordered_action_ids()
    {
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();

        IReadOnlyList<GuidedSurfaceBlock> blocks = AssistantGuidedSurface.Blocks(Result([Action(first), Action(second)]));

        GuidedSurfaceBlock block = blocks.Should().ContainSingle().Subject;
        block.Kind.Should().Be(GuidedSurfaceBlockKind.ActionGroup);
        block.ActionGroup!.AgentRunId.Should().Be("run-1");
        block.ActionGroup.ActionIds.Should().Equal(first, second);
    }

    [Fact]
    public void A_question_becomes_a_question_block()
    {
        AgentInteractiveQuestion question = new("Which area?", [new("a", "A"), new("b", "B")], 1);

        IReadOnlyList<GuidedSurfaceBlock> blocks = AssistantGuidedSurface.Blocks(Result([]) with { Question = question });

        GuidedSurfaceBlock block = blocks.Should().ContainSingle().Subject;
        block.Kind.Should().Be(GuidedSurfaceBlockKind.Question);
        block.Question!.Prompt.Should().Be("Which area?");
        block.Question.Options.Select(option => option.Key).Should().Equal("a", "b");
    }

    [Fact]
    public void A_next_action_becomes_a_next_action_block()
    {
        IReadOnlyList<GuidedSurfaceBlock> blocks = AssistantGuidedSurface.Blocks(Result([]) with { NextAction = nameof(ClinicSetupGoal.StaffUsers) });

        GuidedSurfaceBlock block = blocks.Should().ContainSingle().Subject;
        block.Kind.Should().Be(GuidedSurfaceBlockKind.NextAction);
        block.NextAction.Should().Be(nameof(ClinicSetupGoal.StaffUsers));
    }

    [Fact]
    public void Everything_together_is_ordered_actions_then_question_then_next_action()
    {
        AssistantResult result = Result([Action(Guid.NewGuid())]) with
        {
            Question = new("Which?", [new("a", "A"), new("b", "B")], 1),
            NextAction = nameof(ClinicSetupGoal.ClinicProfile)
        };

        AssistantGuidedSurface.Blocks(result).Select(block => block.Kind).Should().Equal(
            GuidedSurfaceBlockKind.ActionGroup, GuidedSurfaceBlockKind.Question, GuidedSurfaceBlockKind.NextAction);
    }

    private static AssistantResult Result(IReadOnlyList<AssistantAction> actions) =>
        new("run-1", "conversation-1", [], actions);

    private static AssistantAction Action(Guid id) => new(
        id, "run-1", "conversation-1", Guid.Empty, "practice-1", "user-1", "Operations", "activate_shift",
        Guid.NewGuid(), "{}", AssistantActionStatus.Proposed, Now, Now.AddHours(1));
}
