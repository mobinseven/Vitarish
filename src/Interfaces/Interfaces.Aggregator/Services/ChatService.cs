using Injectio.Attributes;
using Vitarish.Interfaces.Aggregator.Abstractions;
using Vitarish.Interfaces.Aggregator.Models;

namespace Vitarish.Interfaces.Aggregator.Services;

[RegisterScoped]

internal class ChatService : IChatService
{
    public event EventHandler<ChatSubmitEventArgs>? OnChatSubmit;
    public void ChatSubmit(string message) => OnChatSubmit?.Invoke(this, new ChatSubmitEventArgs(message));
}
