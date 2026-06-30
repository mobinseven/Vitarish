using Vitarish.Journey.Application.Abstractions;
using Vitarish.Journey.Application.Models;
using Vitarish.Journey.Domain;

namespace Vitarish.Journey.Application.Resolution;

public sealed class JourneyResolver(
    IJourneyProfileReader profileReader,
    IJourneyConsentReader consentReader,
    IJourneyWorkspaceReader workspaceReader,
    IJourneyReturnUrlPolicy returnUrlPolicy,
    IJourneyDestinationResolver destinationResolver) : IJourneyResolver
{
    public async Task<JourneyDecision> ResolveAsync(JourneyRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.UserId)) throw new ArgumentException("UserId is required.", nameof(request));

        JourneyProfileSnapshot profile = await profileReader.ReadAsync(request.UserId, cancellationToken);
        JourneyConsentSnapshot consent = await consentReader.ReadAsync(request.UserId, cancellationToken);
        JourneyWorkspaceSnapshot workspace = await workspaceReader.ReadAsync(request, cancellationToken);

        List<JourneyGate> gates = new();
        if (!profile.HasConfirmedPrimaryContact)
        {
            gates.Add(JourneyGate.Blocking(JourneyGateKind.ContactConfirmation, JourneyPolicy.ContactConfirmationPath));
        }

        if (!profile.IsProfileComplete)
        {
            gates.Add(JourneyGate.Blocking(JourneyGateKind.ProfileCompletion, JourneyPolicy.ProfileCompletionPath));
        }

        if (consent.HasRequiredConsent)
        {
            gates.Add(JourneyGate.Blocking(JourneyGateKind.RequiredConsent, JourneyPolicy.RequiredConsentPath));
        }

        if (workspace.RequiresSelection)
        {
            gates.Add(JourneyGate.Blocking(JourneyGateKind.WorkspaceSelection, JourneyPolicy.WorkspaceSelectionPath));
        }

        string? safeReturnUrl = returnUrlPolicy.Normalize(request.RequestedReturnUrl);
        JourneyStep fallbackStep = safeReturnUrl is not null
            ? new JourneyStep(JourneyDestinationKind.ReturnUrl, safeReturnUrl)
            : new JourneyStep(JourneyDestinationKind.GlobalDashboard, await destinationResolver.ResolveFallbackAsync(request, workspace, cancellationToken));

        return JourneyPolicy.Decide(gates, fallbackStep);
    }
}
