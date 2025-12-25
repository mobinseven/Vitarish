using Vitarish.Accounts.Infrastructure.Services;

namespace Vitarish.Main.Mock.Services;

public sealed class MockMessageQueue : IMessageQueue
{
    public void EnqueueEmail(string to, string subject, string textBody, string htmlBody)
    {
    }

    public void EnqueueSms(string phoneNumber, string code)
    {
    }
}
