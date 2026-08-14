using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Intelligence.Agents;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Intelligence.Tests.Agents;

public sealed class AgentCompositionTests
{
    [Fact]
    public void Composes_inherited_profile_and_multiple_exposed_packs_deterministically()
    {
        AgentCapabilityCatalog<string> catalog = new([new Pack("calendar", 2, "calendar_tool"), new Pack("notes", 4, "notes_tool")]);
        ComposedAgentManifest<string> first = new AgentProfileComposer<string>(catalog).Compose(new DerivedProfile(), "context", _ => new(true, "exposure-1"));
        ComposedAgentManifest<string> second = new AgentProfileComposer<string>(catalog).Compose(new DerivedProfile(), "context", _ => new(true, "exposure-1"));

        first.Definition.Name.Should().Be("Fixture Agent");
        first.Tools.Select(x => x.Definition.Name).Should().Equal("calendar_tool", "notes_tool");
        first.CompatibilityKey.Should().Be(second.CompatibilityKey);
        first.CompatibilityKey.Should().MatchRegex("^[a-f0-9]{64}$");
    }

    [Fact]
    public void Denied_pack_contributes_no_model_surface_but_exposure_revision_is_compatible()
    {
        AgentCapabilityCatalog<string> catalog = new([new Pack("calendar", 2, "calendar_tool"), new Pack("notes", 4, "notes_tool")]);
        AgentProfileComposer<string> composer = new(catalog);
        ComposedAgentManifest<string> denied = composer.Compose(new DerivedProfile(), "context", _ => AgentExposureDecision.Denied("denied-1"));
        ComposedAgentManifest<string> changed = composer.Compose(new DerivedProfile(), "context", _ => AgentExposureDecision.Denied("denied-2"));

        denied.Tools.Should().BeEmpty();
        denied.Definition.Instructions.Should().NotContain("calendar instruction");
        denied.CompatibilityKey.Should().NotBe(changed.CompatibilityKey);
    }

    [Fact]
    public void Schema_formatting_does_not_change_compatibility_key()
    {
        AgentCapabilityCatalog<string> catalog = new([new Pack("calendar", 2, "calendar_tool")]);
        string first = new AgentProfileComposer<string>(catalog).Compose(new SchemaProfile("{\"type\":\"object\",\"properties\":{\"x\":{\"type\":\"string\"}}}"), "context", _ => new(true, "1")).CompatibilityKey;
        string second = new AgentProfileComposer<string>(catalog).Compose(new SchemaProfile("{ \"properties\": { \"x\": { \"type\": \"string\" } }, \"type\": \"object\" }"), "context", _ => new(true, "1")).CompatibilityKey;
        first.Should().Be(second);
    }

    [Fact]
    public void Behavior_revision_and_semantic_instruction_changes_change_compatibility()
    {
        AgentCapabilityCatalog<string> firstCatalog = new([new Pack("calendar", 2, "calendar_tool")]);
        AgentCapabilityCatalog<string> secondCatalog = new([new Pack("calendar", 3, "calendar_tool")]);
        string first = new AgentProfileComposer<string>(firstCatalog).Compose(new SchemaProfile("{\"type\":\"object\"}"), "context", _ => new(true, "1")).CompatibilityKey;
        string second = new AgentProfileComposer<string>(secondCatalog).Compose(new SchemaProfile("{\"type\":\"object\"}"), "context", _ => new(true, "1")).CompatibilityKey;
        first.Should().NotBe(second);
    }

    [Fact]
    public void Capability_instruction_changes_change_compatibility_without_revision_bump()
    {
        AgentCapabilityCatalog<string> firstCatalog = new([new Pack("calendar", 2, "calendar_tool", "first instruction")]);
        AgentCapabilityCatalog<string> secondCatalog = new([new Pack("calendar", 2, "calendar_tool", "changed instruction")]);

        string first = new AgentProfileComposer<string>(firstCatalog).Compose(new SchemaProfile("{\"type\":\"object\"}"), "context", _ => new(true, "1")).CompatibilityKey;
        string second = new AgentProfileComposer<string>(secondCatalog).Compose(new SchemaProfile("{\"type\":\"object\"}"), "context", _ => new(true, "1")).CompatibilityKey;

        first.Should().NotBe(second);
    }

    [Fact]
    public void Registered_generic_tools_are_added_without_a_capability_pack_in_deterministic_order()
    {
        IntelligenceAgentToolCatalog toolCatalog = new([new GenericTool("zeta"), new GenericTool("alpha")]);
        AgentProfileComposer<string> composer = new(new AgentCapabilityCatalog<string>([]), toolCatalog);

        ComposedAgentManifest<string> manifest = composer.Compose(new NoCapabilityProfile(), "context", _ => new(true, "enabled"));

        manifest.Tools.Select(tool => tool.Definition.Name).Should().Equal("alpha", "zeta");
        manifest.ToolCapabilities.Values.Should().OnlyContain(value => value == IntelligenceAgentToolCatalog.CapabilityKey);
    }

    [Fact]
    public void Generic_tool_catalog_rejects_empty_and_duplicate_names()
    {
        FluentActions.Invoking(() => new IntelligenceAgentToolCatalog([new GenericTool(" ")]))
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*non-empty*");

        FluentActions.Invoking(() => new IntelligenceAgentToolCatalog([new GenericTool("duplicate"), new GenericTool("duplicate")]))
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*registered more than once*");
    }

    [Fact]
    public void Generic_tool_definition_changes_change_compatibility()
    {
        AgentProfileComposer<string> firstComposer = new(
            new AgentCapabilityCatalog<string>([]),
            new IntelligenceAgentToolCatalog([new GenericTool("search", "first")]));
        AgentProfileComposer<string> secondComposer = new(
            new AgentCapabilityCatalog<string>([]),
            new IntelligenceAgentToolCatalog([new GenericTool("search", "changed")]));

        string first = firstComposer.Compose(new NoCapabilityProfile(), "context", _ => new(true, "enabled")).CompatibilityKey;
        string second = secondComposer.Compose(new NoCapabilityProfile(), "context", _ => new(true, "enabled")).CompatibilityKey;

        first.Should().NotBe(second);
    }

    [Fact]
    public void DI_registered_generic_tools_are_composed_by_scoped_composers()
    {
        ServiceCollection services = new();
        _ = services.AddSingleton<IUniversalIntelligenceAgentTool>(new GenericTool("search"));
        _ = services.AddAgentComposition<string>();
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope firstScope = provider.CreateScope();
        using IServiceScope secondScope = provider.CreateScope();

        AgentProfileComposer<string> first = firstScope.ServiceProvider.GetRequiredService<AgentProfileComposer<string>>();
        AgentProfileComposer<string> second = secondScope.ServiceProvider.GetRequiredService<AgentProfileComposer<string>>();

        first.Should().NotBeSameAs(second);
        first.Compose(new NoCapabilityProfile(), "context", _ => new(true, "enabled"))
            .Tools.Select(tool => tool.Definition.Name).Should().Equal("search");
    }

    [Fact]
    public void Non_universal_registered_tools_are_not_added_to_profiles()
    {
        ServiceCollection services = new();
        _ = services.AddSingleton<IUniversalIntelligenceAgentTool>(new GenericTool("calendar"));
        _ = services.AddSingleton<IIntelligenceAgentTool>(new NonUniversalTool("search"));
        _ = services.AddAgentComposition<string>();
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        AgentProfileComposer<string> composer = scope.ServiceProvider.GetRequiredService<AgentProfileComposer<string>>();

        composer.Compose(new NoCapabilityProfile(), "context", _ => new(true, "enabled"))
            .Tools.Select(tool => tool.Definition.Name).Should().Equal("calendar");
    }

    private sealed class DerivedProfile : BaseProfile
    {
        protected override void Configure(AgentProfileBuilder<string> builder)
        {
            base.Configure(builder);
            builder.ApplyLayer("derived", 1, layer =>
            {
                layer.SetIdentity("fixture-agent", "Fixture Agent");
                layer.AddCapability("notes");
            });
        }
    }

    private abstract class BaseProfile : AgentProfile<string>
    {
        protected override void Configure(AgentProfileBuilder<string> builder)
        {
            builder.ApplyLayer("base", 1, layer =>
            {
                layer.SetIdentity("fixture-agent", "Fixture Agent");
                layer.SetModel("fixture-model");
                layer.AddInstruction("base", "Base instruction", 10);
                layer.AddCapability("calendar");
            });
        }
    }

    private sealed class SchemaProfile(string schema) : AgentProfile<string>
    {
        protected override void Configure(AgentProfileBuilder<string> builder)
        {
            builder.ApplyLayer("schema", 1, layer =>
            {
                layer.SetIdentity("fixture-agent", "Fixture Agent");
                layer.SetModel("fixture-model");
                layer.SetResult(new("result", schema));
                layer.AddCapability("calendar");
            });
        }
    }

    private sealed class NoCapabilityProfile : AgentProfile<string>
    {
        protected override void Configure(AgentProfileBuilder<string> builder)
        {
            builder.ApplyLayer("empty", 1, layer =>
            {
                layer.SetIdentity("fixture-agent", "Fixture Agent");
                layer.SetModel("fixture-model");
            });
        }
    }

    private sealed class Pack(string key, int revision, string toolName, string? instructionText = null) : IAgentCapabilityPack<string>
    {
        public string Key => key;
        public int BehaviorRevision => revision;
        public AgentCapabilityContribution CreateContribution(string context) => new(
            [new($"{key}.instructions", instructionText ?? $"{key} instruction", 20)],
            [new FixtureTool(toolName)], [], new Dictionary<string, string>());
    }

    private sealed class FixtureTool(string name) : IAgentTool
    {
        public AgentToolDefinition Definition => new(name, "fixture", "{\"type\":\"object\"}");
        public Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AgentToolResult(call.Id, "{}", false));
    }

    private sealed class GenericTool(string name, string? description = null) : IUniversalIntelligenceAgentTool
    {
        public AgentToolDefinition Definition => new(name, description ?? "fixture", "{\"type\":\"object\"}");
        public Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AgentToolResult(call.Id, "{}"));
    }

    private sealed class NonUniversalTool(string name) : IIntelligenceAgentTool
    {
        public AgentToolDefinition Definition => new(name, name, "{\"type\":\"object\"}");
        public Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AgentToolResult(call.Id, "{}"));
    }
}
