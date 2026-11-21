using System.Text.Json;
using FluentAssertions;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Intelligence.Tests.Agents;

public sealed class AgentConversationEndpointRegistryTests
{
    [Fact]
    public void Resolves_the_single_matching_route_without_registration_order_dependence()
    {
        Endpoint clinic = new("clinic.operations", "practice");
        Endpoint secondary = new("sample.agent", "tenant");
        AgentConversationEndpointRegistry registry = new([clinic, secondary]);

        registry.Resolve("sample.agent", "tenant").Should().BeSameAs(secondary);
        registry.Resolve("clinic.operations", "practice").Should().BeSameAs(clinic);
        registry.Resolve("sample.agent", "practice").Should().BeNull();
    }

    [Fact]
    public void Streaming_is_off_by_default_and_the_registered_capability_is_resolvable_per_route()
    {
        AgentConversationEndpointRegistry registry = new([new Endpoint("clinic.operations", "practice"), new StreamingEndpoint()]);

        registry.Routes.Should().BeEquivalentTo(
            [new AgentConversationRoute("clinic.operations", "practice", false), new AgentConversationRoute("sample.agent", "tenant", true)]);
        registry.ResolveRegistration("clinic.operations", "practice")!.Route.StreamingEnabled.Should().BeFalse();
        registry.ResolveRegistration("sample.agent", "tenant")!.Route.StreamingEnabled.Should().BeTrue();
        registry.ResolveRegistration("sample.agent", "practice").Should().BeNull();
    }

    [Fact]
    public void Rejects_duplicate_route_ownership()
    {
        Action construct = () => _ = new AgentConversationEndpointRegistry([new Endpoint("sample.agent", "tenant"), new Endpoint("sample.agent", "tenant")]);

        construct.Should().Throw<InvalidOperationException>().WithMessage("*multiple owners*");
    }

    [Fact]
    public void Runtime_tool_registry_resolves_by_agent_key_and_rejects_duplicates()
    {
        RuntimeEndpoint clinic = new("clinic.operations");
        AgentRuntimeToolEndpointRegistry registry = new([clinic]);
        registry.Resolve("clinic.operations").Should().BeSameAs(clinic);
        registry.Resolve("missing").Should().BeNull();
        Action duplicate = () => _ = new AgentRuntimeToolEndpointRegistry([new RuntimeEndpoint("clinic.operations"), new RuntimeEndpoint("clinic.operations")]);
        duplicate.Should().Throw<InvalidOperationException>().WithMessage("*multiple owners*");
    }

    private sealed class Endpoint(string key, string kind) : IAgentConversationEndpoint
    {
        public IReadOnlyCollection<AgentConversationRoute> Routes { get; } = [new(key, kind)];
        public Task<AgentConversationEndpointResult> RunAsync(AgentConversationEndpointRequest request, CancellationToken cancellationToken) => Task.FromResult(new AgentConversationEndpointResult(AgentConversationOutcome.Succeeded, "run", Guid.Empty, JsonDocument.Parse("{}").RootElement.Clone()));
    }

    private sealed class StreamingEndpoint : IAgentConversationEndpoint
    {
        public IReadOnlyCollection<AgentConversationRoute> Routes { get; } = [new("sample.agent", "tenant", StreamingEnabled: true)];
        public Task<AgentConversationEndpointResult> RunAsync(AgentConversationEndpointRequest request, CancellationToken cancellationToken) => Task.FromResult(new AgentConversationEndpointResult(AgentConversationOutcome.NotFound));
    }

    private sealed class RuntimeEndpoint(string key) : IAgentRuntimeToolEndpoint
    {
        public IReadOnlyCollection<string> AgentKeys { get; } = [key];
        public Task<AgentToolResult> ExecuteAsync(AgentRuntimeToolEndpointRequest request, CancellationToken cancellationToken) => Task.FromResult(new AgentToolResult(request.ToolCall.Id, "{}"));
    }
}
