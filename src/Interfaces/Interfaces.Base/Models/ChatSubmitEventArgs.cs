namespace Vitarish.Interfaces.Base.Models;

public class ChatSubmitEventArgs(string message) : EventArgs
{
    public string Message { get; set; } = message;
}
