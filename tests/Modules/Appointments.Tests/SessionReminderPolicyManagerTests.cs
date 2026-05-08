namespace Appointments.Tests;

public class SessionReminderPolicyManagerTests
{
    private readonly Mock<ISessionReminderPolicyRepository> _repository = new();
    private readonly Mock<IAuditLogger> _auditLogger = new();
    private readonly Mock<IRequestContext> _requestContext = new();

    private SessionReminderPolicyManager CreateSut()
    {
        _requestContext.SetupGet(x => x.UserId).Returns("user-1");
        _requestContext.SetupGet(x => x.TenantId).Returns("tenant-a");
        return new SessionReminderPolicyManager(_repository.Object, _auditLogger.Object, _requestContext.Object);
    }

    [Fact]
    public async Task AddPolicyAsync_ShouldFail_WhenOffsetIsZero()
    {
        var sut = CreateSut();
        var dto = ValidDto() with { OffsetBefore = TimeSpan.Zero };

        var result = await sut.AddPolicyAsync(dto, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message.Contains("positive", StringComparison.OrdinalIgnoreCase));
        _repository.Verify(r => r.AddAsync(It.IsAny<SessionReminderPolicy>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddPolicyAsync_ShouldFail_WhenOffsetIsNegative()
    {
        var sut = CreateSut();
        var dto = ValidDto() with { OffsetBefore = TimeSpan.FromHours(-1) };

        var result = await sut.AddPolicyAsync(dto, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message.Contains("positive", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task AddPolicyAsync_ShouldFail_WhenQualifyingStatusesIsEmpty()
    {
        var sut = CreateSut();
        var dto = ValidDto() with { QualifyingStatuses = [] };

        var result = await sut.AddPolicyAsync(dto, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message.Contains("status", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task AddPolicyAsync_ShouldFail_WhenSubjectTemplateIsEmpty()
    {
        var sut = CreateSut();
        var dto = ValidDto() with { SubjectTemplate = string.Empty };

        var result = await sut.AddPolicyAsync(dto, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message.Contains("subject", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task AddPolicyAsync_ShouldFail_WhenBodyTemplateIsEmpty()
    {
        var sut = CreateSut();
        var dto = ValidDto() with { BodyTemplate = string.Empty };

        var result = await sut.AddPolicyAsync(dto, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message.Contains("body", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task AddPolicyAsync_ShouldSucceed_WhenDtoIsValid()
    {
        var sut = CreateSut();
        var dto = ValidDto();
        var entity = new SessionReminderPolicy
        {
            Id = Guid.NewGuid(),
            SessionId = dto.SessionId,
            Channel = dto.Channel,
            OffsetBefore = dto.OffsetBefore,
            IsEnabled = dto.IsEnabled,
            SubjectTemplate = dto.SubjectTemplate,
            BodyTemplate = dto.BodyTemplate
        };
        entity.QualifyingStatuses = [ReservationStatus.Pending];

        _repository.Setup(r => r.AddAsync(It.IsAny<SessionReminderPolicy>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var result = await sut.AddPolicyAsync(dto, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _repository.Verify(r => r.AddAsync(It.IsAny<SessionReminderPolicy>(), It.IsAny<CancellationToken>()), Times.Once);
        _auditLogger.Verify(a => a.LogAsync(It.Is<AppointmentAuditEntry>(e => e.Action == "Created"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemovePolicyAsync_ShouldFail_WhenPolicyNotFound()
    {
        var sut = CreateSut();
        _repository.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SessionReminderPolicy?)null);

        var result = await sut.RemovePolicyAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task RemovePolicyAsync_ShouldSucceed_WhenPolicyExists()
    {
        var sut = CreateSut();
        var existing = new SessionReminderPolicy { Id = Guid.NewGuid() };
        _repository.Setup(r => r.GetAsync(existing.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _repository.Setup(r => r.RemoveAsync(existing, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await sut.RemovePolicyAsync(existing.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _auditLogger.Verify(a => a.LogAsync(It.Is<AppointmentAuditEntry>(e => e.Action == "Removed"), It.IsAny<CancellationToken>()), Times.Once);
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
