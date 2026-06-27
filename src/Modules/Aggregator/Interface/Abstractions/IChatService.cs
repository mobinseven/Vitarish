namespace Vitarish.Modules.Aggregator.Interface.Abstractions;

public interface IChatService
{
    event EventHandler<ChatSubmitEventArgs>? OnChatSubmit;
    event EventHandler<ChatWaitEventArgs>? OnChatWait;
    event EventHandler<ChatContextEventArgs>? OnChatContext;
    void ChatSubmit(string message);
    void WaitChat();
    void EnableChat();
    void SetChatContext(RenderFragment renderFragment);
}
