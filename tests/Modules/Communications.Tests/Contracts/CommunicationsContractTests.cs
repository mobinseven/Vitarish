using System.Text.Json;
using FluentAssertions;
using Vitarish.Communications.Contracts.Reminders;
using Vitarish.Communications.Contracts.Requests;
using Vitarish.Communications.Contracts.Results;
using Vitarish.Communications.Contracts.Routes;
using Xunit;
namespace Communications.Tests.Contracts;
public sealed class CommunicationsContractTests
{
    [Fact] public void CommandResult_RepresentsAllRequiredStatuses(){ Enum.GetNames<CommunicationsCommandStatus>().Should().Contain([nameof(CommunicationsCommandStatus.Accepted),nameof(CommunicationsCommandStatus.Queued),nameof(CommunicationsCommandStatus.Duplicate),nameof(CommunicationsCommandStatus.ValidationFailure),nameof(CommunicationsCommandStatus.Unauthorized),nameof(CommunicationsCommandStatus.Forbidden),nameof(CommunicationsCommandStatus.ProviderUnavailable),nameof(CommunicationsCommandStatus.ProviderRejected),nameof(CommunicationsCommandStatus.Timeout),nameof(CommunicationsCommandStatus.Cancellation),nameof(CommunicationsCommandStatus.UnexpectedFailure)]); }
    [Fact] public void Contracts_RoundTripThroughJson(){ SendEmailRequest request=new("to@example.test","subject","text","<p>text</p>"); JsonSerializer.Deserialize<SendEmailRequest>(JsonSerializer.Serialize(request)).Should().Be(request); ReminderReadDto reminder=new(Guid.NewGuid(),Guid.NewGuid(),ReminderKind.Email,"user-1","to@example.test","subject","body",DateTime.UtcNow,null,false,"creator",DateTime.UtcNow); JsonSerializer.Deserialize<ReminderReadDto>(JsonSerializer.Serialize(reminder)).Should().Be(reminder); }
    [Fact] public void Routes_DefineStableCommunicationsBase(){ CommunicationsApiRoutes.Base.Should().Be("/communications"); CommunicationsApiRoutes.SendEmail.Should().Be("/messages/email"); CommunicationsApiRoutes.SmsDeliveryState.Should().Contain("delivery-state"); }
}
