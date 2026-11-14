using System.Text.Json.Nodes;
using FluentAssertions;
using Vitarish.Intelligence.Application.Agents;

namespace Intelligence.Tests.Agents;

public sealed class AgentConversationHistoryTests
{
    // Shapes as the agent runtime stores them after a turn that searched and then answered.
    private const string TurnWithTools = """
        [
          {"role":"user","content":"find Sara"},
          {"type":"reasoning","id":"rs_1","content":[]},
          {"type":"function_call","id":"fc_1","callId":"call_1","name":"search_patients","arguments":"{}"},
          {"type":"function_call_result","callId":"call_1","name":"search_patients","output":{"type":"text","text":"[]"}},
          {"type":"message","id":"msg_1","role":"assistant","status":"completed","content":[{"type":"output_text","text":"No patient named Sara."}],"providerData":{"phase":"final"}}
        ]
        """;

    [Fact]
    public void Carry_over_keeps_only_user_and_assistant_messages_without_provider_links()
    {
        JsonArray carried = (JsonArray)JsonNode.Parse(AgentConversationHistory.CarryOver(TurnWithTools)!)!;

        carried.Should().HaveCount(2);
        carried[0]!["role"]!.GetValue<string>().Should().Be("user");
        JsonObject assistant = (JsonObject)carried[1]!;
        assistant["role"]!.GetValue<string>().Should().Be("assistant");
        assistant["content"]![0]!["text"]!.GetValue<string>().Should().Be("No patient named Sara.");
        assistant.ContainsKey("id").Should().BeFalse("a message id links to the dropped reasoning item, which a provider would require");
        assistant.ContainsKey("providerData").Should().BeFalse();
    }

    [Fact]
    public void Carry_over_drops_the_oldest_turns_to_fit_and_starts_with_a_user_turn()
    {
        string history = "[" + string.Join(',', Enumerable.Range(0, 10).Select(index =>
            $$"""{"role":"user","content":"question {{index}} {{new string('x', 100)}}"},{"role":"assistant","content":[{"type":"output_text","text":"answer {{index}}"}]}""")) + "]";

        JsonArray carried = (JsonArray)JsonNode.Parse(AgentConversationHistory.CarryOver(history, maximumBytes: 600)!)!;

        carried.ToJsonString().Length.Should().BeLessThanOrEqualTo(600);
        carried[0]!["role"]!.GetValue<string>().Should().Be("user");
        carried[^1]!["content"]![0]!["text"]!.GetValue<string>().Should().Be("answer 9");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-json")]
    [InlineData("{}")]
    [InlineData("""[{"type":"function_call","name":"x"}]""")]
    public void Carry_over_returns_nothing_when_no_message_can_continue(string? history) =>
        AgentConversationHistory.CarryOver(history).Should().BeNull();
}
