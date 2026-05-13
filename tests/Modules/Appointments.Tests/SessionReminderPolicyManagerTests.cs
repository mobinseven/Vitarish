namespace Appointments.Tests;

public class SessionReminderPolicyManagerTests
{
    private readonly Mock<ISessionAggregateLoader> _aggregateLoader = new();
    private readonly Mock<IAuditWriter> _auditWriter = new();
    private readonly Mock<IRequestContext> _requestContext = new();

    private SessionReminderPolicyManager CreateSut()
    {
        _requestContext.SetupGet(x => x.UserId).Returns("user-1");
        _requestContext.SetupGet(x => x.TenantId).Returns("tenant-a");
        return new SessionReminderPolicyManager(_aggregateLoader.Object, _auditWriter.Object, _requestContext.Object);
    }

    [Fact]
    public async Task AddPolicyAsync_ShouldFail_WhenOffsetIsZero()
    {
        var sut = CreateSut();
        var dto = ValidDto();

        var result = await sut.AddPolicyAsync(dto with { OffsetBefore = TimeSpan.Zero }, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message == "SessionReminderPolicyOffsetBeforePositiveError");
    }

    [Fact]
    public async Task AddPolicyAsync_ShouldFail_WhenOffsetIsNegative()
    {
        var sut = CreateSut();

        var result = await sut.AddPolicyAsync(ValidDto() with { OffsetBefore = TimeSpan.FromHours(-1) }, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message == "SessionReminderPolicyOffsetBeforePositiveError");
    }

    [Fact]
    public async Task AddPolicyAsync_ShouldFail_WhenQualifyingStatusesIsEmpty()
    {
        var sut = CreateSut();

        var result = await sut.AddPolicyAsync(ValidDto() with { QualifyingStatuses = [] }, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message == "SessionReminderPolicyQualifyingStatusesRequiredError");
    }

    [Fact]
    public async Task AddPolicyAsync_ShouldFail_WhenSubjectTemplateIsEmpty()
    {
        var sut = CreateSut();

        var result = await sut.AddPolicyAsync(ValidDto() with { SubjectTemplate = string.Empty }, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message == "SessionReminderPolicySubjectTemplateRequiredError");
    }

    [Fact]
    public async Task AddPolicyAsync_ShouldFail_WhenBodyTemplateIsEmpty()
    {
        var sut = CreateSut();

        var result = await sut.AddPolicyAsync(ValidDto() with { BodyTemplate = string.Empty }, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message == "SessionReminderPolicyBodyTemplateRequiredError");
    }

    [Fact]
    public async Task AddPolicyAsync_ShouldSucceed_WhenDtoIsValid()
    {
        var sut = CreateSut();
        var dto = ValidDto();
        var session = new Session { Id = dto.SessionId };

        _aggregateLoader.Setup(r => r.LoadWithPoliciesAsync(dto.SessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        _aggregateLoader.Setup(r => r.SaveAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await sut.AddPolicyAsync(dto, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _auditWriter.Verify(a => a.WriteAsync(It.Is<AuditEntry>(e => e.Action == "Created"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemovePolicyAsync_ShouldFail_WhenSessionNotFound()
    {
        var sut = CreateSut();
        _aggregateLoader.Setup(r => r.LoadWithPoliciesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Session?)null);

        var result = await sut.RemovePolicyAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message == "SessionReminderPolicySessionNotFoundError");
    }

    [Fact]
    public async Task RemovePolicyAsync_ShouldFail_WhenPolicyNotFound()
    {
        var sut = CreateSut();
        var session = new Session { Id = Guid.NewGuid() };

        _aggregateLoader.Setup(r => r.LoadWithPoliciesAsync(session.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var result = await sut.RemovePolicyAsync(session.Id, Guid.NewGuid(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message == "SessionReminderPolicyNotFoundError");
    }

    [Fact]
    public async Task RemovePolicyAsync_ShouldSucceed_WhenPolicyExists()
    {
        var sut = CreateSut();
        var policyId = Guid.NewGuid();
        var session = new Session { Id = Guid.NewGuid() };
        var policy = new SessionReminderPolicy { Id = policyId, SessionId = session.Id };
        session.ReminderPolicies.Add(policy);

        _aggregateLoader.Setup(r => r.LoadWithPoliciesAsync(session.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        _aggregateLoader.Setup(r => r.SaveAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await sut.RemovePolicyAsync(session.Id, policyId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _auditWriter.Verify(a => a.WriteAsync(It.Is<AuditEntry>(e => e.Action == "Removed"), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static SessionReminderPolicyWriteDto ValidDto() => new()
    {
        SessionId = Guid.NewGuid(),
        Channel = ReminderKind.Email,
        OffsetBefore = TimeSpan.FromHours(1),
        QualifyingStatuses = [ReservationStatus.Pending],
        SubjectTemplate = "Reminder: {{OccurrenceStart}}",
        BodyTemplate = "Your appointment starts at {{OccurrenceStart}}.",
        IsEnabled = true
    };
}
