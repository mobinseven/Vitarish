namespace Appointments.Tests;

public class ShiftReminderPolicyManagerTests
{
    private readonly Mock<IMediator> _mediator = new();
    private readonly Mock<IAuditWriter> _auditWriter = new();
    private readonly Mock<IRequestContext> _requestContext = new();

    private IShiftReminderPolicyManager CreateSut()
    {
        _ = _requestContext.SetupGet(x => x.UserId).Returns("user-1");
        _ = _requestContext.SetupGet(x => x.TenantId).Returns("tenant-a");
        return new ShiftReminderPolicyManager(_mediator.Object, _auditWriter.Object, _requestContext.Object);
    }

    [Fact]
    public async Task AddAsync_ShouldFail_WhenOffsetIsZero()
    {
        IShiftReminderPolicyManager sut = CreateSut();
        ShiftReminderPolicyWriteDto dto = ValidDto();

        EntityResult<ShiftReminderPolicyReadDto> result = await sut.AddAsync(dto with { OffsetBefore = TimeSpan.Zero }, CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Errors.Should().ContainSingle(e => e.Message == "ShiftReminderPolicyOffsetBeforePositiveError");
    }

    [Fact]
    public async Task AddAsync_ShouldFail_WhenOffsetIsNegative()
    {
        IShiftReminderPolicyManager sut = CreateSut();

        EntityResult<ShiftReminderPolicyReadDto> result = await sut.AddAsync(ValidDto() with { OffsetBefore = TimeSpan.FromHours(-1) }, CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Errors.Should().ContainSingle(e => e.Message == "ShiftReminderPolicyOffsetBeforePositiveError");
    }

    [Fact]
    public async Task AddAsync_ShouldFail_WhenQualifyingStatusesIsEmpty()
    {
        IShiftReminderPolicyManager sut = CreateSut();

        EntityResult<ShiftReminderPolicyReadDto> result = await sut.AddAsync(ValidDto() with { QualifyingStatuses = [] }, CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Errors.Should().ContainSingle(e => e.Message == "ShiftReminderPolicyQualifyingStatusesRequiredError");
    }

    [Fact]
    public async Task AddAsync_ShouldFail_WhenSubjectTemplateIsEmpty()
    {
        IShiftReminderPolicyManager sut = CreateSut();

        EntityResult<ShiftReminderPolicyReadDto> result = await sut.AddAsync(ValidDto() with { SubjectTemplate = string.Empty }, CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Errors.Should().ContainSingle(e => e.Message == "ShiftReminderPolicySubjectTemplateRequiredError");
    }

    [Fact]
    public async Task AddAsync_ShouldFail_WhenBodyTemplateIsEmpty()
    {
        IShiftReminderPolicyManager sut = CreateSut();

        EntityResult<ShiftReminderPolicyReadDto> result = await sut.AddAsync(ValidDto() with { BodyTemplate = string.Empty }, CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Errors.Should().ContainSingle(e => e.Message == "ShiftReminderPolicyBodyTemplateRequiredError");
    }

    [Fact]
    public async Task AddAsync_ShouldSucceed_WhenDtoIsValid()
    {
        IShiftReminderPolicyManager sut = CreateSut();
        ShiftReminderPolicyWriteDto dto = ValidDto();
        ShiftReminderPolicy savedPolicy = new()
        { Id = Guid.NewGuid() };

        _ = _mediator.Setup(m => m.Send(It.IsAny<IRequest<ShiftReminderPolicy?>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(savedPolicy);

        EntityResult<ShiftReminderPolicyReadDto> result = await sut.AddAsync(dto, CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _auditWriter.Verify(a => a.WriteAsync(It.Is<AuditEntry>(e => e.Action == "Created"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_ShouldSucceed_WhenPolicyExists()
    {
        IShiftReminderPolicyManager sut = CreateSut();
        Guid policyId = Guid.NewGuid();

        _ = _mediator.Setup(m => m.Send(It.IsAny<IRequest<bool>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        EntityResult result = await sut.RemoveAsync(policyId, CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
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
