using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Clinic.Application.PatientAgent;
using Vitarish.Clinic.Application.PatientAgent.Capabilities;
using Vitarish.Intelligence.Agents;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Clinic.Agents;

public sealed class ClinicWorkspaceAgentComposer(
    AgentProfileComposer<ClinicAgentCompositionContext> profileComposer,
    PatientAgentCapabilityRegistry capabilityRegistry,
    PatientAgentToolDispatcher toolDispatcher,
    PatientWorkspaceAgentOptions options,
    IEnumerable<IIntelligenceAgentTool> intelligenceTools,
    TimeProvider timeProvider) : IPatientWorkspaceAgentComposer
{
    public PatientWorkspaceAgentDefinition Compose(
        PatientAgentContext context,
        AgentExecutionContext? executionContext = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (string.IsNullOrWhiteSpace(options.Model))
            throw new InvalidOperationException("A configured patient workspace agent model is required.");
        if (context.SubjectKind == "patient" && context.PatientId == Guid.Empty)
            throw new ArgumentException("A route-sourced patient id is required.", nameof(context));

        DateTimeOffset currentUtc = timeProvider.GetUtcNow();
        TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(context.TimeZone);
        AgentExecutionContext trustedExecution = executionContext ?? CreateExecutionContext(context, currentUtc, timeZone);
        IReadOnlyCollection<IPatientAgentCapability> available = capabilityRegistry.GetAvailable(context);
        IReadOnlyCollection<IIntelligenceAgentTool> intelligence = intelligenceTools.ToArray();
        ClinicAgentCompositionContext composition = new(context, available, intelligence, currentUtc);
        AgentProfile<ClinicAgentCompositionContext> profile = context.SubjectKind == "practice"
            ? new ClinicOperationsAgentProfile(composition, options)
            : new ClinicPatientAgentProfile(composition, options);
        ComposedAgentManifest<ClinicAgentCompositionContext> manifest = profileComposer.Compose(
            profile,
            composition,
            key => IsExposed(key, available, intelligence)
                ? new AgentExposureDecision(true, "enabled-v1")
                : AgentExposureDecision.Denied("denied-v1"));
        PatientAgentExecutionCollector collector = new();
        IReadOnlyCollection<IAgentTool> tools = toolDispatcher.CreateGuardedTools(
            context, trustedExecution, manifest.Tools, manifest.ToolCapabilities, collector);
        Dictionary<string, IReadOnlySet<string>> actions = available.ToDictionary(
            capability => capability.Name,
            capability => (IReadOnlySet<string>)capability.ActionTypes.ToHashSet(StringComparer.Ordinal),
            StringComparer.Ordinal);
        return new(
            manifest.Definition,
            tools,
            actions,
            collector,
            trustedExecution,
            manifest.Limits,
            manifest.ToolCapabilities,
            manifest.CompatibilityKey);
    }

    private static bool IsExposed(
        string key,
        IReadOnlyCollection<IPatientAgentCapability> available,
        IReadOnlyCollection<IIntelligenceAgentTool> intelligence) =>
        key == ClinicAgentCapabilityKeys.Intelligence
            ? intelligence.Count > 0
            : available.Any(capability => string.Equals(capability.Name, key, StringComparison.Ordinal));

    private static AgentExecutionContext CreateExecutionContext(
        PatientAgentContext context,
        DateTimeOffset currentUtc,
        TimeZoneInfo timeZone)
    {
        DateTimeOffset currentLocal = TimeZoneInfo.ConvertTime(currentUtc, timeZone);
        Dictionary<string, string> metadata = new(StringComparer.Ordinal)
        {
            ["practiceId"] = context.PracticeId,
            ["patientId"] = context.PatientId.ToString("D"),
            ["authenticatedUserId"] = context.AuthenticatedUserId,
            ["permissions"] = JsonSerializer.Serialize(context.Permissions),
            ["role"] = context.Role,
            ["locale"] = context.Locale,
            ["calendar"] = context.Calendar,
            ["timeZone"] = context.TimeZone,
            ["currentUtc"] = currentUtc.ToString("O"),
            ["currentLocal"] = currentLocal.ToString("O"),
            ["conversationId"] = context.ConversationId,
            ["subjectKind"] = context.SubjectKind,
            ["agentKey"] = context.SubjectKind == "practice" ? "clinic.operations" : "clinic.patient"
        };
        if (!string.IsNullOrWhiteSpace(context.PatientNationalId)) metadata["patientNationalId"] = context.PatientNationalId;
        return new(Guid.NewGuid().ToString("N"), metadata);
    }
}

public static class ClinicAgentCompositionServiceCollectionExtensions
{
    public static IServiceCollection AddClinicAgentComposition(this IServiceCollection services)
    {
        _ = services.AddAgentComposition<ClinicAgentCompositionContext>(
            new ClinicAgentCapabilityPack(ClinicAgentCapabilityKeys.Appointments),
            new ClinicAgentCapabilityPack(ClinicAgentCapabilityKeys.AppointmentReads),
            new ClinicAgentCapabilityPack(ClinicAgentCapabilityKeys.Prescriptions),
            new ClinicAgentCapabilityPack(ClinicAgentCapabilityKeys.PrescriptionReads),
            new ClinicAgentCapabilityPack(ClinicAgentCapabilityKeys.PatientReminderReads),
            new ClinicAgentCapabilityPack(ClinicAgentCapabilityKeys.ClinicPatientReads),
            new ClinicAgentCapabilityPack(ClinicAgentCapabilityKeys.SeaPatientReads),
            new ClinicAgentCapabilityPack(ClinicAgentCapabilityKeys.ClinicalEvents),
            new ClinicAgentCapabilityPack(ClinicAgentCapabilityKeys.SeaManagement),
            new ClinicAgentCapabilityPack(ClinicAgentCapabilityKeys.Operations),
            new ClinicIntelligenceCapabilityPack());
        _ = services.AddTransient<IPatientWorkspaceAgentComposer, ClinicWorkspaceAgentComposer>();
        return services;
    }
}
