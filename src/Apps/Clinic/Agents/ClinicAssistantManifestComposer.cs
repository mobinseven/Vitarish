using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Vitarish.Clinic.Application.Assistant;
using Vitarish.Clinic.Application.Assistant.Capabilities;
using Vitarish.Intelligence.Agents;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Clinic.Agents;

public sealed class ClinicAssistantManifestComposer(
    AgentProfileComposer<ClinicAssistantCompositionContext> profileComposer,
    AssistantCapabilityRegistry capabilityRegistry,
    AssistantToolDispatcher toolDispatcher,
    ClinicAssistantOptions options,
    IEnumerable<IIntelligenceAgentTool> intelligenceTools,
    ClinicAssistantExecutionContextFactory executionContextFactory,
    ILogger<ClinicAssistantManifestComposer>? logger = null)
{
    internal ClinicAssistantManifest Compose(
        AssistantContext context,
        AgentExecutionContext? suppliedExecutionContext = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (string.IsNullOrWhiteSpace(options.Model))
            throw new InvalidOperationException("A configured clinic assistant model is required.");
        if (context.SubjectKind == "patient" && context.PatientId == Guid.Empty)
            throw new ArgumentException("A route-sourced patient id is required.", nameof(context));
        if (context.SubjectKind is not ("patient" or "practice"))
            throw new ArgumentException("The Clinic assistant subject kind is invalid.", nameof(context));

        AssistantContext trustedContext = suppliedExecutionContext is null
            ? context
            : executionContextFactory.ValidateAndRebuild(context, suppliedExecutionContext);
        AgentExecutionContext executionContext = suppliedExecutionContext ?? executionContextFactory.Create(context);

        IReadOnlyCollection<IAssistantCapability> available = capabilityRegistry.GetAvailable(trustedContext);
        IReadOnlyCollection<IIntelligenceAgentTool> intelligence = intelligenceTools.ToArray();
        IReadOnlyCollection<string> profileKeys = trustedContext.SubjectKind == "practice"
            ? ClinicAssistantCapabilityKeys.OperationsProfile
            : ClinicAssistantCapabilityKeys.Patient;
        IReadOnlyCollection<IAssistantCapability> profileCapabilities = available
            .Where(capability => profileKeys.Contains(capability.Name, StringComparer.Ordinal))
            .ToArray();
        ClinicAssistantCompositionContext composition = new(trustedContext, available, intelligence, executionContextFactory.CurrentUtc);
        AgentProfile<ClinicAssistantCompositionContext> profile = trustedContext.SubjectKind == "practice"
            ? new ClinicOperationsAssistantProfile(composition, options)
            : new ClinicPatientAssistantProfile(composition, options);
        ComposedAgentManifest<ClinicAssistantCompositionContext> manifest = profileComposer.Compose(
            profile,
            composition,
            key => IsExposed(key, available, intelligence)
                ? new AgentExposureDecision(true, "enabled-v1")
                : AgentExposureDecision.Denied("denied-v1"));
        if (logger is not null)
        {
            foreach (string key in profileKeys)
            {
                bool enabled = IsExposed(key, available, intelligence);
                ClinicAssistantTelemetry.CapabilityDecision(
                    logger,
                    key,
                    enabled ? "enabled" : "denied",
                    enabled ? "authorized_and_available" : "not_available_or_unauthorized");
            }
        }
        IReadOnlyCollection<IAgentTool> tools = toolDispatcher.CreateGuardedTools(
            trustedContext,
            executionContext,
            manifest.Tools,
            manifest.ToolCapabilities);
        Dictionary<string, IReadOnlySet<string>> actions = profileCapabilities.ToDictionary(
            capability => capability.Name,
            capability => (IReadOnlySet<string>)capability.ActionPolicies
                .Where(policy => policy.Authorizes(trustedContext))
                .Select(policy => policy.ActionType)
                .ToHashSet(StringComparer.Ordinal),
            StringComparer.Ordinal);
        logger?.LogInformation(
            "Clinic assistant composition completed for {SubjectKind} with {CapabilityCount} capabilities and {ToolCount} tools; compatibility {CompatibilityKey}.",
            trustedContext.SubjectKind, available.Count, tools.Count, manifest.CompatibilityKey);
        return new(manifest.Definition, tools, actions, new(), executionContext, manifest.Limits, manifest.ToolCapabilities, manifest.CompatibilityKey);

    }

    private static bool IsExposed(
        string key,
        IReadOnlyCollection<IAssistantCapability> available,
        IReadOnlyCollection<IIntelligenceAgentTool> intelligence) =>
        key == ClinicAssistantCapabilityKeys.Intelligence
            ? intelligence.Count > 0
            : available.Any(capability => string.Equals(capability.Name, key, StringComparison.Ordinal));
}

internal sealed record ClinicAssistantManifest(
    AgentDefinition Definition,
    IReadOnlyCollection<IAgentTool> Tools,
    IReadOnlyDictionary<string, IReadOnlySet<string>> EnabledActions,
    ClinicAssistantExecutionCollector ExecutionCollector,
    AgentExecutionContext ExecutionContext,
    AgentExecutionLimits Limits,
    IReadOnlyDictionary<string, string> ToolCapabilities,
    string CompatibilityKey);
