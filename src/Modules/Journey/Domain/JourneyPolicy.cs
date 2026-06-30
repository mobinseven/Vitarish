namespace Vitarish.Journey.Domain;

public static class JourneyPolicy
{
    public const string ContactConfirmationPath = "/account/contact/confirm";
    public const string ProfileCompletionPath = "/account/profile/complete";
    public const string RequiredConsentPath = "/account/consents";
    public const string WorkspaceSelectionPath = "/workspaces/select";
    public const string GlobalDashboardPath = "/dashboard";

    public static JourneyDecision Decide(IEnumerable<JourneyGate> gates, JourneyStep fallbackStep)
    {
        ArgumentNullException.ThrowIfNull(gates);
        ArgumentNullException.ThrowIfNull(fallbackStep);

        List<JourneyGate> evaluatedGates = gates.ToList();
        JourneyGate? blockingGate = evaluatedGates.FirstOrDefault(gate => gate.IsBlocking);
        JourneyStep step = blockingGate is null
            ? fallbackStep
            : new JourneyStep(JourneyDestinationKind.Gate, blockingGate.DestinationUrl);

        return new JourneyDecision(step, evaluatedGates);
    }
}
