using Vitarish.Journey.Application.Abstractions;
using Vitarish.Journey.Application.Rules;
using Vitarish.Journey.Domain;
using Vitarish.Users.Application.ProfileCompletion;

namespace Vitarish.Journey.Application;

/// <summary>
/// Resolves the next valid destination for an authenticated user.
/// Priority order:
/// 1. Account recovery / disabled account (delegated to Authentication)
/// 2. Suspicious login / reauthentication (delegated to Authentication)
/// 3. Required contact confirmation
/// 4. Required profile completion
/// 5. Required legal consent
/// 6. Required workspace or app selection
/// 7. Safe requested return URL
/// 8. Last used app or workspace dashboard
/// 9. User home
/// 10. Global dashboard
/// </summary>
public sealed class JourneyResolver(
    IJourneyProfileReader profileReader,
    IJourneyConsentReader consentReader,
    IJourneyWorkspaceReader workspaceReader,
    IJourneyReturnUrlPolicy returnUrlPolicy,
    IJourneyDestinationResolver destinationResolver,
    IProfileCompletionPolicy profileCompletionPolicy
) : IJourneyResolver
{
    public async Task<JourneyStep> ResolveAsync(JourneyRequest request, CancellationToken cancellationToken = default)
    {
        var nonBlockingGates = new List<JourneyGate>();

        // Read snapshots
        var profile = await profileReader.GetSnapshotAsync(request.UserId, cancellationToken);
        var consent = await consentReader.GetSnapshotAsync(request.UserId, cancellationToken);
        var workspace = await workspaceReader.GetSnapshotAsync(request.UserId, cancellationToken);

        // Priority 3: Contact confirmation
        if (profile is not null)
        {
            var contactGate = JourneyPolicy.EvaluateContactConfirmation(profile);
            if (contactGate is not null)
                return new JourneyStep(JourneyDestinationKind.Gate, contactGate.DestinationUrl, contactGate);
        }

        // Priority 4: Profile completion (delegates to Users/Profile policy)
        var completionResult = await profileCompletionPolicy.EvaluateAsync(request.UserId, cancellationToken);
        if (!completionResult.IsComplete)
        {
            var missingFields = string.Join(", ", completionResult.MissingRequirements.Select(r => r.FieldName));
            var profileGate = new JourneyGate(
                JourneyGateKind.ProfileCompletion,
                IsBlocking: true,
                DestinationUrl: "/account/profile/complete",
                Message: $"Missing required profile fields: {missingFields}"
            );
            return new JourneyStep(JourneyDestinationKind.Gate, profileGate.DestinationUrl, profileGate);
        }

        // Priority 5: Required consent
        var consentGate = JourneyPolicy.EvaluateConsent(consent);
        if (consentGate is not null)
            return new JourneyStep(JourneyDestinationKind.Gate, consentGate.DestinationUrl, consentGate);

        // Priority 6: Workspace selection
        var workspaceGate = JourneyPolicy.EvaluateWorkspaceSelection(workspace);
        if (workspaceGate is not null)
            return new JourneyStep(JourneyDestinationKind.Gate, workspaceGate.DestinationUrl, workspaceGate);

        // Priority 7: Safe return URL
        var safeReturnUrl = returnUrlPolicy.Normalize(request.RequestedReturnUrl);
        if (safeReturnUrl is not null)
            return new JourneyStep(JourneyDestinationKind.ReturnUrl, safeReturnUrl);

        // Priority 8: Last used app/workspace dashboard
        if (workspace?.LastUsedAppKey is not null)
        {
            var appDashboardUrl = destinationResolver.GetAppDashboardUrl(workspace.LastUsedAppKey);
            return new JourneyStep(JourneyDestinationKind.AppDashboard, appDashboardUrl);
        }

        if (workspace?.LastUsedWorkspaceId is not null)
        {
            var workspaceDashboardUrl = destinationResolver.GetWorkspaceDashboardUrl(workspace.LastUsedWorkspaceId);
            return new JourneyStep(JourneyDestinationKind.AppDashboard, workspaceDashboardUrl);
        }

        // Priority 9: User home
        var userHomeUrl = destinationResolver.GetUserHomeUrl(request.UserId);
        return new JourneyStep(JourneyDestinationKind.UserHome, userHomeUrl);
    }
}
