using Vitarish.Intelligence.Agents;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Intelligence.Application.GuidedSurfaces;
using Vitarish.Clinic.Application.Assistant;
using Vitarish.Clinic.Application.Assistant.Operations;
using Vitarish.Clinic.Application.Assistant.Staff;
using Vitarish.Search.Agents;
using Vitarish.Contents.Agents;
using Vitarish.Clinic.Application.Assistant.Capabilities;

namespace Vitarish.Clinic.Agents;

public static class AssistantCapabilityKeys
{
    public const string Appointments = "Appointments";
    public const string AppointmentReads = "AppointmentReads";
    public const string Prescriptions = "Prescriptions";
    public const string PrescriptionReads = "PrescriptionReads";
    public const string PatientReminderReads = "PatientReminderReads";
    public const string ClinicPatientReads = "ClinicPatientReads";
    public const string SeaPatientReads = "SeaPatientReads";
    public const string ClinicalEvents = "ClinicalEvents";
    public const string Surgeries = "Surgeries";
    public const string SurgeryOperations = "SurgeryOperations";
    public const string SeaManagement = "SeaManagement";
    public const string Operations = "Operations";
    public const string Search = "Search";
    public const string Contents = "Contents";
    public const string Setup = Vitarish.Clinic.Application.Setup.ClinicSetupAssistant.CapabilityName;
    public const string Staff = AssistantStaffInvitation.CapabilityKey;
    public const string Presentation = "Presentation";

    public static readonly string[] Patient =
    [Appointments, AppointmentReads, Prescriptions, PrescriptionReads, PatientReminderReads,
        ClinicPatientReads, SeaPatientReads, ClinicalEvents, Surgeries, Presentation];

    public static readonly string[] OperationsProfile = [Operations, SeaManagement, SurgeryOperations, Search, Contents, Setup, Staff, Presentation];

    /// <summary>Every key a profile composes; each one needs a registered capability pack.</summary>
    public static IEnumerable<string> All => Patient.Concat(OperationsProfile).Distinct(StringComparer.Ordinal);
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

public sealed class SearchAssistantCapability(IEnumerable<ISearchAgentTool> tools) : AssistantCapability([.. tools])
{
    public override string Name => AssistantCapabilityKeys.Search;
    public override string Description => "Search the authenticated practice's approved indexes.";
    public override IReadOnlySet<string> RequiredPermissions { get; } = new HashSet<string>(StringComparer.Ordinal);
    public override bool IsAvailable(AssistantContext context) =>
        context.SubjectKind == "practice" && AssistantOperationsScope.IsAuthorizedRole(context.Role);
}

public sealed class ContentsAssistantCapability(IEnumerable<IContentsAgentTool> tools) : AssistantCapability(
    [.. tools],
    tools.Any() ? [ContentsAssistantActionPolicy.Create(ContentsCreateDraftEntryAgentTool.Name, ContentsSaveDraftAgentTool.Name)] : [])
{
    public override string Name => AssistantCapabilityKeys.Contents;
    public override string Description => "Review content and author drafts for the authenticated practice.";
    public override IReadOnlySet<string> RequiredPermissions { get; } = new HashSet<string>(StringComparer.Ordinal);
    public override bool IsAvailable(AssistantContext context) =>
        Tools.Count > 0 && context.SubjectKind == "practice" &&
        string.Equals(context.Role, AssistantOperationsScope.TenantOwnerRole, StringComparison.Ordinal);
}

/// <summary>Lets both Clinic assistants show records they read as presented surfaces (ADR-0022 decision 8).</summary>
public sealed class PresentationAssistantCapability(PresentSurfaceTool tool) : AssistantCapability([tool])
{
    public override string Name => AssistantCapabilityKeys.Presentation;
    public override string Description => "Show records read in this turn as cards, lists, numbers or charts.";
    public override IReadOnlySet<string> RequiredPermissions { get; } = new HashSet<string>(StringComparer.Ordinal);
    public override bool IsAvailable(AssistantContext context) => true;

    public override string GetInstructions() =>
        "Whenever a read tool returned records the user asked to see (patients, appointments, availability, shifts, surgeries, staff, setup progress, a count or a patient's own records), " +
        $"call {PresentSurfaceTool.Name} in the same turn with the handle from that result's presentation before you answer. " +
        "Use a RecordList for several records, a RecordCard for one, a Metric for a count and a BarChart only for counts compared across groups. " +
        "Then answer in one or two short sentences that point to what is shown; do not repeat the shown records in text. " +
        "Skip it only when the read found nothing, when the read only resolved IDs for another tool, or when the user asked a yes/no question.";
}
