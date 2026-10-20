using Moq;
using Vitarish.Journey.Application;
using Vitarish.Journey.Application.Abstractions;
using Vitarish.Journey.Domain;
using Vitarish.Users.Application.ProfileCompletion;

namespace Journey.Tests;

public class JourneyResolverTests
{
    private readonly Mock<IJourneyProfileReader> _profileReaderMock = new();
    private readonly Mock<IJourneyConsentReader> _consentReaderMock = new();
    private readonly Mock<IJourneyWorkspaceReader> _workspaceReaderMock = new();
    private readonly Mock<IJourneyReturnUrlPolicy> _returnUrlPolicyMock = new();
    private readonly Mock<IJourneyDestinationResolver> _destinationResolverMock = new();
    private readonly Mock<IProfileCompletionPolicy> _profileCompletionPolicyMock = new();
    private readonly Mock<IJourneyHostReader> _hostReaderMock = new();
    private readonly JourneyResolver _sut;

    public JourneyResolverTests()
    {
        _sut = new JourneyResolver(
            _profileReaderMock.Object,
            _consentReaderMock.Object,
            _workspaceReaderMock.Object,
            _returnUrlPolicyMock.Object,
            _destinationResolverMock.Object,
            _profileCompletionPolicyMock.Object,
            _hostReaderMock.Object);
    }

    [Fact]
    public async Task ResolveAsync_ContactConfirmationRequired_ReturnsContactConfirmationGate()
    {
        // Arrange
        var request = new JourneyRequest("user1", null, "/", null, null, true);

        _profileReaderMock.Setup(x => x.GetSnapshotAsync("user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JourneyProfileSnapshot("user1", "John", "Doe", "John Doe", null, null, false, false));

        // Act
        var result = await _sut.ResolveAsync(request);

        // Assert
        result.HasBlockingGate.Should().BeTrue();
        result.BlockingGate!.Kind.Should().Be(JourneyGateKind.ContactConfirmation);
        result.DestinationUrl.Should().Be("/account/contact/confirm");
    }

    [Fact]
    public async Task ResolveAsync_ProfileIncomplete_ReturnsProfileCompletionGate()
    {
        // Arrange
        var request = new JourneyRequest("user1", null, "/", null, null, true);

        _profileReaderMock.Setup(x => x.GetSnapshotAsync("user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JourneyProfileSnapshot("user1", null, null, null, null, null, true, false));

        _profileCompletionPolicyMock.Setup(x => x.EvaluateAsync("user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProfileCompletionResult.Incomplete(
                new ProfileCompletionRequirement("FirstName", true)));

        // Act
        var result = await _sut.ResolveAsync(request);

        // Assert
        result.HasBlockingGate.Should().BeTrue();
        result.BlockingGate!.Kind.Should().Be(JourneyGateKind.ProfileCompletion);
        result.DestinationUrl.Should().Be("/account/profile/complete");
    }

    [Fact]
    public async Task ResolveAsync_ConsentRequired_ReturnsConsentGate()
    {
        // Arrange
        var request = new JourneyRequest("user1", null, "/", null, null, true);

        _profileReaderMock.Setup(x => x.GetSnapshotAsync("user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JourneyProfileSnapshot("user1", "John", "Doe", "John Doe", null, null, true, false));

        _profileCompletionPolicyMock.Setup(x => x.EvaluateAsync("user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProfileCompletionResult.Complete);

        _consentReaderMock.Setup(x => x.GetSnapshotAsync("user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JourneyConsentSnapshot(false, true));

        // Act
        var result = await _sut.ResolveAsync(request);

        // Assert
        result.HasBlockingGate.Should().BeTrue();
        result.BlockingGate!.Kind.Should().Be(JourneyGateKind.RequiredConsent);
        result.DestinationUrl.Should().Be("/account/consents");
    }

    [Fact]
    public async Task ResolveAsync_OnTheSignUpHost_SendsACompleteAccountToTenantCreationBeforeAnyReturnUrl()
    {
        var request = new JourneyRequest("user1", "/patients", "/", null, null, true);
        SetUpCompleteAccount();
        _hostReaderMock.Setup(x => x.IsSignUpHostAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _returnUrlPolicyMock.Setup(x => x.Normalize("/patients")).Returns("/patients");

        var result = await _sut.ResolveAsync(request);

        result.HasBlockingGate.Should().BeTrue();
        result.BlockingGate!.Kind.Should().Be(JourneyGateKind.TenantCreation);
        result.DestinationUrl.Should().Be("/setup");
    }

    [Fact]
    public async Task ResolveAsync_OnTheSignUpHost_ProfileCompletionStillComesFirst()
    {
        var request = new JourneyRequest("user1", null, "/", null, null, true);
        _profileReaderMock.Setup(x => x.GetSnapshotAsync("user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JourneyProfileSnapshot("user1", null, null, null, null, null, true, false));
        _profileCompletionPolicyMock.Setup(x => x.EvaluateAsync("user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProfileCompletionResult.Incomplete(new ProfileCompletionRequirement("FirstName", true)));
        _hostReaderMock.Setup(x => x.IsSignUpHostAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _sut.ResolveAsync(request);

        result.BlockingGate!.Kind.Should().Be(JourneyGateKind.ProfileCompletion);
    }

    [Fact]
    public async Task ResolveAsync_OffTheSignUpHost_NeverAsksForTenantCreation()
    {
        var request = new JourneyRequest("user1", null, "/", null, null, true);
        SetUpCompleteAccount();
        _hostReaderMock.Setup(x => x.IsSignUpHostAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await _sut.ResolveAsync(request);

        result.BlockingGate?.Kind.Should().NotBe(JourneyGateKind.TenantCreation);
    }

    private void SetUpCompleteAccount()
    {
        _profileReaderMock.Setup(x => x.GetSnapshotAsync("user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JourneyProfileSnapshot("user1", "John", "Doe", "John Doe", null, null, true, false));
        _profileCompletionPolicyMock.Setup(x => x.EvaluateAsync("user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProfileCompletionResult.Complete);
        _consentReaderMock.Setup(x => x.GetSnapshotAsync("user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JourneyConsentSnapshot(true, true));
    }

    [Fact]
    public async Task ResolveAsync_WorkspaceSelectionRequired_ReturnsWorkspaceSelectionGate()
    {
        // Arrange
        var request = new JourneyRequest("user1", null, "/", null, null, true);

        _profileReaderMock.Setup(x => x.GetSnapshotAsync("user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JourneyProfileSnapshot("user1", "John", "Doe", "John Doe", null, null, true, false));

        _profileCompletionPolicyMock.Setup(x => x.EvaluateAsync("user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProfileCompletionResult.Complete);

        _consentReaderMock.Setup(x => x.GetSnapshotAsync("user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JourneyConsentSnapshot(true, true));

        _workspaceReaderMock.Setup(x => x.GetSnapshotAsync("user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JourneyWorkspaceSnapshot(null, null, null, null, true));

        // Act
        var result = await _sut.ResolveAsync(request);

        // Assert
        result.HasBlockingGate.Should().BeTrue();
        result.BlockingGate!.Kind.Should().Be(JourneyGateKind.WorkspaceSelection);
        result.DestinationUrl.Should().Be("/workspaces/select");
    }

    [Fact]
    public async Task ResolveAsync_SafeReturnUrl_ReturnsReturnUrl()
    {
        // Arrange
        var request = new JourneyRequest("user1", "/dashboard", "/", null, null, true);

        _profileReaderMock.Setup(x => x.GetSnapshotAsync("user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JourneyProfileSnapshot("user1", "John", "Doe", "John Doe", null, null, true, false));

        _profileCompletionPolicyMock.Setup(x => x.EvaluateAsync("user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProfileCompletionResult.Complete);

        _consentReaderMock.Setup(x => x.GetSnapshotAsync("user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JourneyConsentSnapshot(true, true));

        _workspaceReaderMock.Setup(x => x.GetSnapshotAsync("user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JourneyWorkspaceSnapshot("ws1", "app1", null, null, false));

        _returnUrlPolicyMock.Setup(x => x.Normalize("/dashboard"))
            .Returns("/dashboard");

        // Act
        var result = await _sut.ResolveAsync(request);

        // Assert
        result.HasBlockingGate.Should().BeFalse();
        result.DestinationKind.Should().Be(JourneyDestinationKind.ReturnUrl);
        result.DestinationUrl.Should().Be("/dashboard");
    }

    [Fact]
    public async Task ResolveAsync_UnsafeReturnUrl_IgnoredAndFallsBack()
    {
        // Arrange
        var request = new JourneyRequest("user1", "https://evil.com", "/", null, null, true);

        _profileReaderMock.Setup(x => x.GetSnapshotAsync("user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JourneyProfileSnapshot("user1", "John", "Doe", "John Doe", null, null, true, false));

        _profileCompletionPolicyMock.Setup(x => x.EvaluateAsync("user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProfileCompletionResult.Complete);

        _consentReaderMock.Setup(x => x.GetSnapshotAsync("user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JourneyConsentSnapshot(true, true));

        _workspaceReaderMock.Setup(x => x.GetSnapshotAsync("user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JourneyWorkspaceSnapshot("ws1", null, null, "app1", false));

        _returnUrlPolicyMock.Setup(x => x.Normalize("https://evil.com"))
            .Returns((string?)null);

        _destinationResolverMock.Setup(x => x.GetAppDashboardUrl("app1"))
            .Returns("/apps/app1/dashboard");

        // Act
        var result = await _sut.ResolveAsync(request);

        // Assert
        result.HasBlockingGate.Should().BeFalse();
        result.DestinationKind.Should().Be(JourneyDestinationKind.AppDashboard);
        result.DestinationUrl.Should().Be("/apps/app1/dashboard");
    }

    [Fact]
    public async Task ResolveAsync_NoBetterDestination_ReturnsUserHome()
    {
        // Arrange
        var request = new JourneyRequest("user1", null, "/", null, null, true);

        _profileReaderMock.Setup(x => x.GetSnapshotAsync("user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JourneyProfileSnapshot("user1", "John", "Doe", "John Doe", null, null, true, false));

        _profileCompletionPolicyMock.Setup(x => x.EvaluateAsync("user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProfileCompletionResult.Complete);

        _consentReaderMock.Setup(x => x.GetSnapshotAsync("user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JourneyConsentSnapshot(true, true));

        _workspaceReaderMock.Setup(x => x.GetSnapshotAsync("user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JourneyWorkspaceSnapshot(null, null, null, null, false));

        _destinationResolverMock.Setup(x => x.GetUserHomeUrl("user1"))
            .Returns("/home");

        // Act
        var result = await _sut.ResolveAsync(request);

        // Assert
        result.HasBlockingGate.Should().BeFalse();
        result.DestinationKind.Should().Be(JourneyDestinationKind.UserHome);
        result.DestinationUrl.Should().Be("/home");
    }

    [Fact]
    public async Task ResolveAsync_ContactConfirmationBeatsProfileCompletion()
    {
        // Arrange - Both contact confirmation and profile completion needed
        var request = new JourneyRequest("user1", null, "/", null, null, true);

        _profileReaderMock.Setup(x => x.GetSnapshotAsync("user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JourneyProfileSnapshot("user1", null, null, null, null, null, false, false));

        // Act - Contact confirmation should win (higher priority)
        var result = await _sut.ResolveAsync(request);

        // Assert
        result.HasBlockingGate.Should().BeTrue();
        result.BlockingGate!.Kind.Should().Be(JourneyGateKind.ContactConfirmation);
    }
}
