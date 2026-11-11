using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Intelligence.Application.GuidedSurfaces;

namespace Vitarish.Intelligence.Tests.GuidedSurfaces;

public sealed class GuidedSurfaceBlockTests
{
    private static readonly AgentInteractiveQuestion SampleQuestion = new(
        "Which area first?",
        [new("services", "Services"), new("staff", "Staff")],
        1);

    [Fact]
    public void Model_question_sets_only_the_question_member()
    {
        GuidedSurfaceBlock block = GuidedSurfaceBlock.ModelQuestion(SampleQuestion);

        block.Kind.Should().Be(GuidedSurfaceBlockKind.Question);
        block.NextAction.Should().BeNull();
        block.ActionGroup.Should().BeNull();
        block.Question.Should().NotBeNull();
        block.Question!.Prompt.Should().Be("Which area first?");
        block.Question.MaxSelections.Should().Be(1);
        block.Question.Options.Should().Equal(new GuidedSurfaceOption("services", "Services"), new GuidedSurfaceOption("staff", "Staff"));
    }

    [Fact]
    public void Model_question_rejects_null()
    {
        FluentActions.Invoking(() => GuidedSurfaceBlock.ModelQuestion(null!)).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Next_action_sets_only_the_key_member()
    {
        GuidedSurfaceBlock block = GuidedSurfaceBlock.NextActionKey("StaffUsers");

        block.Kind.Should().Be(GuidedSurfaceBlockKind.NextAction);
        block.NextAction.Should().Be("StaffUsers");
        block.Question.Should().BeNull();
        block.ActionGroup.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Next_action_rejects_a_missing_or_blank_key(string? key)
    {
        FluentActions.Invoking(() => GuidedSurfaceBlock.NextActionKey(key!)).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Prepared_actions_set_only_the_action_group_member()
    {
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();

        GuidedSurfaceBlock block = GuidedSurfaceBlock.PreparedActions("run-1", [first, second]);

        block.Kind.Should().Be(GuidedSurfaceBlockKind.ActionGroup);
        block.Question.Should().BeNull();
        block.NextAction.Should().BeNull();
        block.ActionGroup!.AgentRunId.Should().Be("run-1");
        block.ActionGroup.ActionIds.Should().Equal(first, second);
    }

    [Fact]
    public void Prepared_actions_drop_duplicate_ids_and_keep_order()
    {
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();

        GuidedSurfaceBlock block = GuidedSurfaceBlock.PreparedActions("run-1", [first, second, first]);

        block.ActionGroup!.ActionIds.Should().Equal(first, second);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Prepared_actions_reject_a_missing_or_blank_run_id(string? runId)
    {
        FluentActions.Invoking(() => GuidedSurfaceBlock.PreparedActions(runId!, [Guid.NewGuid()])).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Prepared_actions_reject_a_null_or_empty_id_list()
    {
        FluentActions.Invoking(() => GuidedSurfaceBlock.PreparedActions("run-1", null!)).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => GuidedSurfaceBlock.PreparedActions("run-1", [])).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Read_question_returns_a_valid_question()
    {
        JsonElement root = Json("""{"question":{"prompt":"Pick one","options":[{"key":"a","label":"Alpha"},{"key":"b-2","label":"Beta"}],"maxSelections":2}}""");

        AgentInteractiveQuestion? question = AgentInteractiveReplyFormat.ReadQuestion(root);

        question.Should().NotBeNull();
        question!.Prompt.Should().Be("Pick one");
        question.Options.Should().Equal(new AgentInteractiveOption("a", "Alpha"), new AgentInteractiveOption("b-2", "Beta"));
        question.MaxSelections.Should().Be(2);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-3, 1)]
    [InlineData(99, 2)]
    public void Read_question_clamps_max_selections_to_the_option_count(int requested, int expected)
    {
        JsonElement root = Json("""{"question":{"prompt":"Pick","options":[{"key":"a","label":"A"},{"key":"b","label":"B"}],"maxSelections":""" + requested + "}}");

        AgentInteractiveReplyFormat.ReadQuestion(root)!.MaxSelections.Should().Be(expected);
    }

    [Theory]
    [InlineData("""{}""")]
    [InlineData("""{"question":null}""")]
    [InlineData("""{"question":"text"}""")]
    [InlineData("""{"question":{"options":[{"key":"a","label":"A"},{"key":"b","label":"B"}],"maxSelections":1}}""")]
    [InlineData("""{"question":{"prompt":"  ","options":[{"key":"a","label":"A"},{"key":"b","label":"B"}],"maxSelections":1}}""")]
    [InlineData("""{"question":{"prompt":"Pick","maxSelections":1}}""")]
    [InlineData("""{"question":{"prompt":"Pick","options":"nope","maxSelections":1}}""")]
    [InlineData("""{"question":{"prompt":"Pick","options":[{"key":"a","label":"A"}],"maxSelections":1}}""")]
    [InlineData("""{"question":{"prompt":"Pick","options":[{"key":"a","label":"A"},{"key":"a","label":"Again"}],"maxSelections":1}}""")]
    [InlineData("""{"question":{"prompt":"Pick","options":[{"key":"A B","label":"A"},{"key":"b","label":"B"}],"maxSelections":1}}""")]
    public void Read_question_returns_null_for_a_missing_or_malformed_question(string json)
    {
        AgentInteractiveReplyFormat.ReadQuestion(Json(json)).Should().BeNull();
    }

    [Fact]
    public void Read_question_returns_null_when_the_root_is_not_an_object()
    {
        AgentInteractiveReplyFormat.ReadQuestion(Json("[]")).Should().BeNull();
    }

    [Fact]
    public void Read_next_action_keeps_only_keys_in_the_allowed_set()
    {
        string[] keys = ["StaffUsers", "ClinicProfile"];

        AgentInteractiveReplyFormat.ReadNextAction(Json("""{"nextAction":"StaffUsers"}"""), keys).Should().Be("StaffUsers");
        AgentInteractiveReplyFormat.ReadNextAction(Json("""{"nextAction":"Unknown"}"""), keys).Should().BeNull();
        AgentInteractiveReplyFormat.ReadNextAction(Json("""{"nextAction":"staffusers"}"""), keys).Should().BeNull();
    }

    [Theory]
    [InlineData("""{"nextAction":null}""")]
    [InlineData("""{"nextAction":5}""")]
    [InlineData("""{"nextAction":["StaffUsers"]}""")]
    [InlineData("""{"nextAction":{"key":"StaffUsers"}}""")]
    [InlineData("""{}""")]
    [InlineData("""[]""")]
    public void Read_next_action_drops_non_strings_and_missing_values(string json)
    {
        AgentInteractiveReplyFormat.ReadNextAction(Json(json), ["StaffUsers"]).Should().BeNull();
    }

    [Fact]
    public void Question_schema_is_a_nullable_strict_object_with_prompt_options_and_max_selections()
    {
        JsonObject schema = AgentInteractiveReplyFormat.QuestionSchema();

        schema["type"]!.AsArray().Select(node => node!.GetValue<string>()).Should().Equal("object", "null");
        schema["additionalProperties"]!.GetValue<bool>().Should().BeFalse();
        schema["required"]!.AsArray().Select(node => node!.GetValue<string>())
            .Should().Equal(
                AgentInteractiveReplyFormat.PromptProperty,
                AgentInteractiveReplyFormat.OptionsProperty,
                AgentInteractiveReplyFormat.MaxSelectionsProperty);
        JsonObject properties = schema["properties"]!.AsObject();
        properties[AgentInteractiveReplyFormat.PromptProperty]!["type"]!.GetValue<string>().Should().Be("string");
        properties[AgentInteractiveReplyFormat.MaxSelectionsProperty]!["type"]!.GetValue<string>().Should().Be("integer");
        JsonObject options = properties[AgentInteractiveReplyFormat.OptionsProperty]!.AsObject();
        options["type"]!.GetValue<string>().Should().Be("array");
        options["items"]!["required"]!.AsArray().Select(node => node!.GetValue<string>())
            .Should().Equal(AgentInteractiveReplyFormat.OptionKeyProperty, AgentInteractiveReplyFormat.OptionLabelProperty);
    }

    [Fact]
    public void Next_action_schema_is_a_nullable_string_enum_of_the_keys_plus_null()
    {
        JsonObject schema = AgentInteractiveReplyFormat.NextActionSchema(["StaffUsers", "ClinicProfile"]);

        schema["type"]!.AsArray().Select(node => node!.GetValue<string>()).Should().Equal("string", "null");
        JsonArray values = schema["enum"]!.AsArray();
        values.Should().HaveCount(3);
        values.Take(2).Select(node => node!.GetValue<string>()).Should().Equal("StaffUsers", "ClinicProfile");
        values[2].Should().BeNull();
    }

    [Fact]
    public void Next_action_schema_without_keys_has_no_enum()
    {
        JsonObject schema = AgentInteractiveReplyFormat.NextActionSchema([]);

        schema.ContainsKey("enum").Should().BeFalse();
    }

    private static JsonElement Json(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
