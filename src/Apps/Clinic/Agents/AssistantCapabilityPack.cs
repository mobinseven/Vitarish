using Vitarish.Intelligence.Agents;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Clinic.Application.Assistant.Capabilities;

namespace Vitarish.Clinic.Agents;

public static class AssistantCapabilityKeys
{
    public const string Intelligence = "Intelligence";
    public const string Appointments = "Appointments";
    public const string AppointmentReads = "AppointmentReads";
    public const string Prescriptions = "Prescriptions";
    public const string PrescriptionReads = "PrescriptionReads";
    public const string PatientReminderReads = "PatientReminderReads";
    public const string ClinicPatientReads = "ClinicPatientReads";
    public const string SeaPatientReads = "SeaPatientReads";
    public const string ClinicalEvents = "ClinicalEvents";
    public const string SeaManagement = "SeaManagement";
    public const string Operations = "Operations";

    public static readonly string[] Patient =
    [Appointments, AppointmentReads, Prescriptions, PrescriptionReads, PatientReminderReads,
        ClinicPatientReads, SeaPatientReads, ClinicalEvents, Intelligence];

    public static readonly string[] OperationsProfile = [Operations, SeaManagement, Intelligence];
}

public sealed class AssistantCapabilityPack(string key) : IAgentCapabilityPack<AssistantCompositionContext>
{
    public string Key { get; } = string.IsNullOrWhiteSpace(key)
        ? throw new ArgumentException("A Clinic capability key is required.", nameof(key))
        : key;

    public int BehaviorRevision => 1;

    public AgentCapabilityContribution CreateContribution(AssistantCompositionContext context)
    {
        Vitarish.Clinic.Application.Assistant.Capabilities.IAssistantCapability? capability =
            context.AvailableCapabilities.SingleOrDefault(item => string.Equals(item.Name, Key, StringComparison.Ordinal));
        if (capability is null)
            return new([], [], []);

        AssistantActionPolicy? PolicyFor(string toolName) => capability.ActionPolicies
            .SingleOrDefault(policy => policy.ToolNames.Contains(toolName));
        bool IsExposed(IAgentTool tool) => PolicyFor(tool.Definition.Name) is not { } policy || policy.Authorizes(context.Context);
        IReadOnlyCollection<IAgentTool> tools = capability.Tools.Where(IsExposed).ToArray();
        IReadOnlyCollection<string> actions = capability.ActionPolicies
            .Where(policy => policy.Authorizes(context.Context))
            .Select(policy => policy.ActionType)
            .ToArray();
        return new([], tools.ToArray(), actions.ToArray());
    }
}

public sealed class ClinicIntelligenceCapabilityPack : IAgentCapabilityPack<AssistantCompositionContext>
{
    public string Key => AssistantCapabilityKeys.Intelligence;
    public int BehaviorRevision => 1;

    public AgentCapabilityContribution CreateContribution(AssistantCompositionContext context) =>
        new([], context.IntelligenceTools.Cast<IAgentTool>().ToArray(), []);
}
