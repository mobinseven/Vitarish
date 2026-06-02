using Vitarish.Communications.Application.Abstractions;

namespace Vitarish.Main.Mock.Services;

public sealed class MockMessageQueue : ICommunicationCommandQueue
{
    public void Enqueue(CommunicationCommand command) => ArgumentNullException.ThrowIfNull(command);
}
