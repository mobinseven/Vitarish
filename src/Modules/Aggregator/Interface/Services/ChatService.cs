namespace Vitarish.Modules.Aggregator.Interface.Services;

[RegisterScoped]

internal class ChatService : IChatService
{
    public event EventHandler<ChatSubmitEventArgs>? OnChatSubmit;
    public event EventHandler<ChatWaitEventArgs>? OnChatWait;
    public event EventHandler<ChatContextEventArgs>? OnChatContext;
    public void ChatSubmit(string message) => OnChatSubmit?.Invoke(this, new ChatSubmitEventArgs(message));
    public void EnableChat() => OnChatWait?.Invoke(this, new ChatWaitEventArgs(false));
    public void WaitChat() => OnChatWait?.Invoke(this, new ChatWaitEventArgs(true));
    public void SetChatContext(RenderFragment renderFragment) => OnChatContext?.Invoke(this, new ChatContextEventArgs(renderFragment));
}
