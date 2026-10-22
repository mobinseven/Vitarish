using System.Text.Json;
using FluentAssertions;
using Vitarish.Intelligence.Application.Agents;

namespace Intelligence.Tests.Agents;

public sealed class AgentInteractiveReplyFormatTests
{
    private static readonly AgentInteractionVocabulary Vocabulary = new(["online-booking", "reminders"], ["open-clinic"]);

    [Fact]
    public void Read_keeps_a_valid_reply_with_its_question_references_and_action()
    {
        AgentInteractiveReply? reply = AgentInteractiveReplyFormat.Read(Run("""
            {"text":"Tell me about your clinic.","question":{"prompt":"What kind?","options":[{"key":"dental","label":"Dental"},{"key":"physio","label":"Physiotherapy"}],"maxSelections":1},"references":["online-booking"],"nextAction":"open-clinic"}
            """), Vocabulary);

        reply.Should().NotBeNull();
        reply!.Text.Should().Be("Tell me about your clinic.");
        reply.Question!.Options.Select(option => option.Key).Should().Equal("dental", "physio");
        reply.References.Should().Equal("online-booking");
        reply.NextAction.Should().Be("open-clinic");
    }

    [Fact]
    public void Read_drops_references_and_actions_the_app_did_not_declare()
    {
        AgentInteractiveReply? reply = AgentInteractiveReplyFormat.Read(Run("""
            {"text":"Here is what fits.","question":null,"references":["online-booking","invented-feature","online-booking"],"nextAction":"delete-everything"}
            """), Vocabulary);

        reply!.References.Should().Equal("online-booking");
        reply.NextAction.Should().BeNull();
    }

    [Theory]
    [InlineData("""{"prompt":"Pick one","options":[{"key":"only","label":"Only one"}],"maxSelections":1}""")]
    [InlineData("""{"prompt":"","options":[{"key":"a","label":"A"},{"key":"b","label":"B"}],"maxSelections":1}""")]
    [InlineData("""{"prompt":"Pick","options":[{"key":"Not A Key","label":"A"},{"key":"b","label":"B"}],"maxSelections":1}""")]
    public void Read_removes_a_question_that_cannot_be_answered_by_tapping(string question)
    {
        AgentInteractiveReply? reply = AgentInteractiveReplyFormat.Read(
            Run($$"""{"text":"Still useful text.","question":{{question}},"references":[],"nextAction":null}"""),
            Vocabulary);

        reply!.Text.Should().Be("Still useful text.");
        reply.Question.Should().BeNull();
    }

    [Fact]
    public void Read_caps_selections_to_the_offered_options_and_text_to_its_limit()
    {
        string longText = new('a', AgentInteractiveReplyFormat.MaxTextLength + 50);
        AgentInteractiveReply? reply = AgentInteractiveReplyFormat.Read(Run($$"""
            {"text":"{{longText}}","question":{"prompt":"Pick","options":[{"key":"a","label":"A"},{"key":"b","label":"B"}],"maxSelections":9},"references":[],"nextAction":null}
            """), Vocabulary);

        reply!.Text.Length.Should().Be(AgentInteractiveReplyFormat.MaxTextLength + 1);
        reply.Question!.MaxSelections.Should().Be(2);
    }

    [Fact]
    public void Read_falls_back_to_plain_text_when_no_structured_output_arrives()
    {
        AgentInteractiveReply? reply = AgentInteractiveReplyFormat.Read(
            new AgentRunResult(true, "A plain answer.", null, new(1, 1), TimeSpan.Zero, 1, 0),
            Vocabulary);

        reply.Should().BeEquivalentTo(new AgentInteractiveReply("A plain answer.", null, [], null));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("{\"text\":")]
    public void Read_refuses_output_without_usable_text(string? output)
    {
        AgentInteractiveReplyFormat.Read(new AgentRunResult(true, output, null, new(1, 1), TimeSpan.Zero, 1, 0), Vocabulary)
            .Should().BeNull();
    }

    [Fact]
    public void Definition_requires_every_property_and_limits_keys_to_the_vocabulary()
    {
        AgentResultDefinition definition = AgentInteractiveReplyFormat.Definition(Vocabulary);

        using JsonDocument schema = JsonDocument.Parse(definition.JsonSchema);
        JsonElement root = schema.RootElement;
        root.GetProperty("additionalProperties").GetBoolean().Should().BeFalse();
        root.GetProperty("required").EnumerateArray().Select(item => item.GetString())
            .Should().BeEquivalentTo("text", "question", "references", "nextAction");
        root.GetProperty("properties").GetProperty("references").GetProperty("items").GetProperty("enum")
            .EnumerateArray().Select(item => item.GetString()).Should().Equal("online-booking", "reminders");
        definition.Strict.Should().BeTrue();
    }

    private static AgentRunResult Run(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return new AgentRunResult(true, json, document.RootElement.Clone(), new(1, 1), TimeSpan.Zero, 1, 0);
    }
}
