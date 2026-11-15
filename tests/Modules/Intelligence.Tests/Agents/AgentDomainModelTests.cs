using FluentAssertions;
using Vitarish.Intelligence.Agents;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Intelligence.Tests.Agents;

public sealed class AgentDomainModelTests
{
    private static AgentDomainModel Model() => new("Fixture",
    [
        new("Case", "A piece of work for one client.", ["matter"], ["Has ordered tasks."],
            operations: [new("list", ["list_cases"])]),
        new("Task", "One ordered item of a case.", ["item", "مرحله"], ["Belongs to one case."],
            [new("done", "Finished and accepted.", ["closed"])],
            ["a calendar entry"],
            [
                new("read", ["get_task", "get_task_fallback"], "includes notes"),
                new("finish", ["finish_task"], whenUnavailable: "ask the user to finish it on the task screen")
            ])
    ], ["Never guess a task."]);

    [Fact]
    public void A_word_claimed_by_two_concepts_is_rejected()
    {
        Action act = () => _ = new AgentDomainModel("Fixture", [new("Case", "One.", ["item"]), new("Task", "Two.", ["Item"])]);

        act.Should().Throw<ArgumentException>().WithMessage("*claimed by both*");
    }

    [Fact]
    public void A_repeated_concept_or_a_word_naming_another_concept_is_rejected()
    {
        Action repeated = () => _ = new AgentDomainModel("Fixture", [new("Case", "One."), new("case", "Two.")]);
        Action naming = () => _ = new AgentDomainModel("Fixture", [new("Case", "One.", ["task"]), new("Task", "Two.")]);

        repeated.Should().Throw<ArgumentException>().WithMessage("*more than once*");
        naming.Should().Throw<ArgumentException>().WithMessage("*name of another concept*");
    }

    [Fact]
    public void Exposed_operations_name_their_first_exposed_tool_and_others_are_stated_unavailable()
    {
        string text = AgentDomainModelInstructions.Render(Model(), ["list_cases", "get_task_fallback"]);

        text.Should().Contain("- To list: list_cases");
        text.Should().Contain("- To read: get_task_fallback (includes notes)");
        text.Should().Contain("- To finish: not available in this conversation; ask the user to finish it on the task screen");
        text.Should().NotContain("finish_task");
        text.Should().Contain("Users may say: \"item\", \"مرحله\"");
        text.Should().Contain("- State \"done\": Finished and accepted. Users may say: \"closed\".");
        text.Should().Contain("- Not to be confused with: a calendar entry");
        text.Should().Contain("- Never guess a task.");
        AgentDomainModelInstructions.Render(Model(), ["get_task_fallback", "list_cases"]).Should().Be(text);
    }

    [Fact]
    public void The_model_lists_every_tool_its_operations_name()
    {
        Model().ToolNames.Should().BeEquivalentTo("list_cases", "get_task", "get_task_fallback", "finish_task");
    }

    [Fact]
    public void The_composer_adds_the_domain_section_from_the_tools_it_exposes()
    {
        AgentCapabilityCatalog<string> catalog = new([new Pack("cases", "list_cases")]);
        AgentProfileComposer<string> composer = new(catalog);

        ComposedAgentManifest<string> exposed = composer.Compose(new DomainProfile(Model()), "context", _ => new(true, "1"));
        ComposedAgentManifest<string> denied = composer.Compose(new DomainProfile(Model()), "context", _ => AgentExposureDecision.Denied("1"));

        exposed.Definition.Instructions.Should().Contain("Domain model: Fixture").And.Contain("- To list: list_cases");
        denied.Definition.Instructions.Should().Contain("- To list: not available in this conversation");
        exposed.CompatibilityKey.Should().NotBe(denied.CompatibilityKey);
    }

    private sealed class DomainProfile(AgentDomainModel model) : AgentProfile<string>
    {
        protected override void Configure(AgentProfileBuilder<string> builder) =>
            builder.ApplyLayer("domain", 1, layer =>
            {
                layer.SetIdentity("fixture-agent", "Fixture Agent");
                layer.SetModel("fixture-model");
                layer.SetDomainModel(model, 1);
                layer.AddCapability("cases");
            });
    }

    private sealed class Pack(string key, string toolName) : IAgentCapabilityPack<string>
    {
        public string Key => key;
        public int BehaviorRevision => 1;
        public AgentCapabilityContribution CreateContribution(string context) => new([], [new Tool(toolName)], []);
    }

    private sealed class Tool(string name) : IAgentTool
    {
        public AgentToolDefinition Definition { get; } = new(name, "Fixture tool.", "{\"type\":\"object\"}");
        public Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AgentToolResult(call.Id, "{}"));
    }
}
