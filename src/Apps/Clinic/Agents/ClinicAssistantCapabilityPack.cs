using Vitarish.Intelligence.Agents;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Clinic.Agents;

public static class ClinicAssistantCapabilityKeys
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

public sealed class ClinicAssistantCapabilityPack(string key) : IAgentCapabilityPack<ClinicAssistantCompositionContext>
{
    public string Key { get; } = string.IsNullOrWhiteSpace(key)
        ? throw new ArgumentException("A Clinic capability key is required.", nameof(key))
        : key;

    public int BehaviorRevision => 1;

    public AgentCapabilityContribution CreateContribution(ClinicAssistantCompositionContext context)
    {
        Vitarish.Clinic.Application.ClinicWorkspace.Capabilities.IClinicWorkspaceCapability? capability =
            context.AvailableCapabilities.SingleOrDefault(item => string.Equals(item.Name, Key, StringComparison.Ordinal));
        return capability is null
            ? new([], [], [])
            : new([], capability.Tools.ToArray(), capability.ActionTypes.ToArray());
    }
}

public sealed class ClinicIntelligenceCapabilityPack : IAgentCapabilityPack<ClinicAssistantCompositionContext>
{
    public string Key => ClinicAssistantCapabilityKeys.Intelligence;
    public int BehaviorRevision => 1;

    public AgentCapabilityContribution CreateContribution(ClinicAssistantCompositionContext context) =>
        new([], context.IntelligenceTools.Cast<IAgentTool>().ToArray(), []);
}
