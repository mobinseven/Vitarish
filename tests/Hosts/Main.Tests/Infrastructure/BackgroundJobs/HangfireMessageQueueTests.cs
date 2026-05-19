using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Moq;
using Vitarish.Communications.Application.Abstractions;
using Vitarish.Main.Infrastructure.BackgroundJobs.Communications;

namespace Main.Tests.Infrastructure.BackgroundJobs;

public class HangfireMessageQueueTests
{
    private readonly Mock<IBackgroundJobClient> _mockBackgroundJobClient;
    private readonly HangfireMessageQueue _sut;

    public HangfireMessageQueueTests()
    {
        _mockBackgroundJobClient = new Mock<IBackgroundJobClient>();
        _sut = new HangfireMessageQueue(_mockBackgroundJobClient.Object);
    }

    [Theory]
    [MemberData(nameof(GetValidCommands))]
    public void Enqueue_WithValidCommand_EnqueuesJob(CommunicationCommand command)
    {
        _sut.Enqueue(command);

        _mockBackgroundJobClient.Verify(
            x => x.Create(
                It.Is<Job>(job =>
                    job.Type == typeof(MessageProcessor) &&
                    job.Method.Name == nameof(MessageProcessor.ProcessCommand) &&
                    ReferenceEquals(job.Args[0], command)),
                It.IsAny<EnqueuedState>()),
            Times.Once);
    }

    [Fact]
    public void Enqueue_WithNullCommand_ThrowsArgumentNullException()
    {
        _ = Assert.Throws<ArgumentNullException>(() => _sut.Enqueue(null!));
    }

    [Theory]
    [MemberData(nameof(GetInvalidCommands))]
    public void Enqueue_WithInvalidCommand_ThrowsArgumentException(CommunicationCommand command)
    {
        _ = Assert.Throws<ArgumentException>(() => _sut.Enqueue(command));
    }

    public static IEnumerable<object[]> GetValidCommands()
    {
        yield return [new EmailCommunicationCommand("test@example.com", "Test Subject", "Text Body", "<p>Html Body</p>")];
        yield return [new SmsCommunicationCommand("+1234567890", "Test SMS")];
        yield return [new SmsCodeCommunicationCommand("+1234567890", "123456")];
        yield return [new SmsReminderCommunicationCommand("+1234567890", "Project", "Procedure", "Event", "Tenant")];
    }

    public static IEnumerable<object[]> GetInvalidCommands()
    {
        yield return [new EmailCommunicationCommand("", "Test Subject", "Text Body", "<p>Html Body</p>")];
        yield return [new EmailCommunicationCommand("test@example.com", "", "Text Body", "<p>Html Body</p>")];
        yield return [new SmsCommunicationCommand("", "Test SMS")];
        yield return [new SmsCommunicationCommand("+1234567890", "")];
        yield return [new SmsCodeCommunicationCommand("", "123456")];
        yield return [new SmsCodeCommunicationCommand("+1234567890", "")];
        yield return [new SmsReminderCommunicationCommand("")];
    }
}
