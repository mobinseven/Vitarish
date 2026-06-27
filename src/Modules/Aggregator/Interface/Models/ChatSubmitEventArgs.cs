namespace Vitarish.Modules.Aggregator.Interface.Models;

public class ChatSubmitEventArgs(string message) : EventArgs
{
    public string Message { get; set; } = message;
}
