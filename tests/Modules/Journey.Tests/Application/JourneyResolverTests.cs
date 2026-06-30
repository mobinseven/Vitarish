using Vitarish.Journey.Application.Abstractions;
using Vitarish.Journey.Application.Models;
using Vitarish.Journey.Application.Policies;
using Vitarish.Journey.Application.Resolution;
using Vitarish.Journey.Domain;

namespace Vitarish.Journey.Tests.Application;

public class JourneyResolverTests
{
    [Fact]
    public async Task ResolveAsync_contact_confirmation_beats_profile_completion()
    {
        JourneyDecision decision = await CreateResolver(profile: new(false, false)).ResolveAsync(Request(), CancellationToken.None);

        decision.DestinationUrl.Should().Be(JourneyPolicy.ContactConfirmationPath);
        decision.BlockingGate!.Kind.Should().Be(JourneyGateKind.ContactConfirmation);
    }

    [Fact]
    public async Task ResolveAsync_profile_completion_beats_consent()
    {
        JourneyDecision decision = await CreateResolver(profile: new(true, false), consent: new(true)).ResolveAsync(Request(), CancellationToken.None);

        decision.DestinationUrl.Should().Be(JourneyPolicy.ProfileCompletionPath);
        decision.BlockingGate!.Kind.Should().Be(JourneyGateKind.ProfileCompletion);
    }

    [Fact]
    public async Task ResolveAsync_consent_beats_workspace_selection()
    {
        JourneyDecision decision = await CreateResolver(consent: new(true), workspace: new(true, null)).ResolveAsync(Request(), CancellationToken.None);

        decision.DestinationUrl.Should().Be(JourneyPolicy.RequiredConsentPath);
        decision.BlockingGate!.Kind.Should().Be(JourneyGateKind.RequiredConsent);
    }

    [Fact]
    public async Task ResolveAsync_workspace_selection_beats_return_url()
    {
        JourneyDecision decision = await CreateResolver(workspace: new(true, null)).ResolveAsync(Request("/home"), CancellationToken.None);

        decision.DestinationUrl.Should().Be(JourneyPolicy.WorkspaceSelectionPath);
        decision.BlockingGate!.Kind.Should().Be(JourneyGateKind.WorkspaceSelection);
    }

    [Fact]
    public async Task ResolveAsync_safe_return_url_wins_after_gates_pass()
    {
        JourneyDecision decision = await CreateResolver().ResolveAsync(Request("/home"), CancellationToken.None);

        decision.DestinationUrl.Should().Be("/home");
        decision.IsBlocked.Should().BeFalse();
    }

    [Fact]
    public async Task ResolveAsync_unsafe_return_url_is_ignored()
    {
        JourneyDecision decision = await CreateResolver().ResolveAsync(Request("https://external.example"), CancellationToken.None);

        decision.DestinationUrl.Should().Be(JourneyPolicy.GlobalDashboardPath);
    }

    [Fact]
    public async Task ResolveAsync_dashboard_fallback_is_returned_when_no_better_destination_exists()
    {
        JourneyDecision decision = await CreateResolver().ResolveAsync(Request(), CancellationToken.None);

        decision.DestinationUrl.Should().Be(JourneyPolicy.GlobalDashboardPath);
    }

    private static JourneyRequest Request(string? returnUrl = null) => new("user-1", returnUrl, null, null, null, true);

    private static JourneyResolver CreateResolver(
        JourneyProfileSnapshot? profile = null,
        JourneyConsentSnapshot? consent = null,
        JourneyWorkspaceSnapshot? workspace = null) => new(
            new StubProfileReader(profile ?? new(true, true)),
            new StubConsentReader(consent ?? new(false)),
            new StubWorkspaceReader(workspace ?? new(false, null)),
            new JourneyReturnUrlPolicy(),
            new DefaultJourneyDestinationResolver());

    private sealed class StubProfileReader(JourneyProfileSnapshot snapshot) : IJourneyProfileReader
    {
        public Task<JourneyProfileSnapshot> ReadAsync(string userId, CancellationToken cancellationToken) => Task.FromResult(snapshot);
    }

    private sealed class StubConsentReader(JourneyConsentSnapshot snapshot) : IJourneyConsentReader
    {
        public Task<JourneyConsentSnapshot> ReadAsync(string userId, CancellationToken cancellationToken) => Task.FromResult(snapshot);
    }

    private sealed class StubWorkspaceReader(JourneyWorkspaceSnapshot snapshot) : IJourneyWorkspaceReader
    {
        public Task<JourneyWorkspaceSnapshot> ReadAsync(JourneyRequest request, CancellationToken cancellationToken) => Task.FromResult(snapshot);
    }
}
