using Vitarish.Interfaces.Aggregator.Models;

namespace Vitarish.Interfaces.Aggregator.Abstractions;

public interface IChatService
{
    event EventHandler<ChatSubmitEventArgs>? OnChatSubmit;
    void ChatSubmit(string message);
}
