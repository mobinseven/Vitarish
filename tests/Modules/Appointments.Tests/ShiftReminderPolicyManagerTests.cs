namespace Appointments.Tests;

public class ShiftReminderPolicyManagerTests
{
    private readonly Mock<IShiftAggregateLoader> _aggregateLoader = new();
    private readonly Mock<IAuditWriter> _auditWriter = new();
    private readonly Mock<IRequestContext> _requestContext = new();

    private ShiftReminderPolicyManager CreateSut()
    {
        _requestContext.SetupGet(x => x.UserId).Returns("user-1");
        _requestContext.SetupGet(x => x.TenantId).Returns("tenant-a");
        return new ShiftReminderPolicyManager(_aggregateLoader.Object, _auditWriter.Object, _requestContext.Object);
    }

    [Fact]
    public async Task AddPolicyAsync_ShouldFail_WhenOffsetIsZero()
    {
        var sut = CreateSut();
        var dto = ValidDto();

        var result = await sut.AddPolicyAsync(dto with { OffsetBefore = TimeSpan.Zero }, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message == "ShiftReminderPolicyOffsetBeforePositiveError");
    }

    [Fact]
    public async Task AddPolicyAsync_ShouldFail_WhenOffsetIsNegative()
    {
        var sut = CreateSut();

        var result = await sut.AddPolicyAsync(ValidDto() with { OffsetBefore = TimeSpan.FromHours(-1) }, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message == "ShiftReminderPolicyOffsetBeforePositiveError");
    }

    [Fact]
    public async Task AddPolicyAsync_ShouldFail_WhenQualifyingStatusesIsEmpty()
    {
        var sut = CreateSut();

        var result = await sut.AddPolicyAsync(ValidDto() with { QualifyingStatuses = [] }, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message == "ShiftReminderPolicyQualifyingStatusesRequiredError");
    }

    [Fact]
    public async Task AddPolicyAsync_ShouldFail_WhenSubjectTemplateIsEmpty()
    {
        var sut = CreateSut();

        var result = await sut.AddPolicyAsync(ValidDto() with { SubjectTemplate = string.Empty }, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message == "ShiftReminderPolicySubjectTemplateRequiredError");
    }

    [Fact]
    public async Task AddPolicyAsync_ShouldFail_WhenBodyTemplateIsEmpty()
    {
        var sut = CreateSut();

        var result = await sut.AddPolicyAsync(ValidDto() with { BodyTemplate = string.Empty }, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message == "ShiftReminderPolicyBodyTemplateRequiredError");
    }

    [Fact]
    public async Task AddPolicyAsync_ShouldSucceed_WhenDtoIsValid()
    {
        var sut = CreateSut();
        var dto = ValidDto();
        var shift = new Shift { Id = dto.ShiftId };

        _aggregateLoader.Setup(r => r.LoadWithPoliciesAsync(dto.ShiftId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shift);
        _aggregateLoader.Setup(r => r.SaveAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await sut.AddPolicyAsync(dto, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _auditWriter.Verify(a => a.WriteAsync(It.Is<AuditEntry>(e => e.Action == "Created"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemovePolicyAsync_ShouldFail_WhenShiftNotFound()
    {
        var sut = CreateSut();
        _aggregateLoader.Setup(r => r.LoadWithPoliciesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Shift?)null);

        var result = await sut.RemovePolicyAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message == "ShiftReminderPolicyShiftNotFoundError");
    }

    [Fact]
    public async Task RemovePolicyAsync_ShouldFail_WhenPolicyNotFound()
    {
        var sut = CreateSut();
        var shift = new Shift { Id = Guid.NewGuid() };

        _aggregateLoader.Setup(r => r.LoadWithPoliciesAsync(shift.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shift);

        var result = await sut.RemovePolicyAsync(shift.Id, Guid.NewGuid(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message == "ShiftReminderPolicyNotFoundError");
    }

    [Fact]
    public async Task RemovePolicyAsync_ShouldSucceed_WhenPolicyExists()
    {
        var sut = CreateSut();
        var policyId = Guid.NewGuid();
        var shift = new Shift { Id = Guid.NewGuid() };
        var policy = new ShiftReminderPolicy { Id = policyId, ShiftId = shift.Id };
        shift.ReminderPolicies.Add(policy);

        _aggregateLoader.Setup(r => r.LoadWithPoliciesAsync(shift.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shift);
        _aggregateLoader.Setup(r => r.SaveAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await sut.RemovePolicyAsync(shift.Id, policyId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _auditWriter.Verify(a => a.WriteAsync(It.Is<AuditEntry>(e => e.Action == "Removed"), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static ShiftReminderPolicyWriteDto ValidDto() => new()
    {
        ShiftId = Guid.NewGuid(),
        Channel = ReminderKind.Email,
        OffsetBefore = TimeSpan.FromHours(1),
        QualifyingStatuses = [ReservationStatus.Pending],
        SubjectTemplate = "Reminder: {{OccurrenceStart}}",
        BodyTemplate = "Your appointment starts at {{OccurrenceStart}}.",
        IsEnabled = true
    };
}
