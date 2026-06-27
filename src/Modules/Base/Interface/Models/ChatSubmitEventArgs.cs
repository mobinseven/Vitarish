namespace Vitarish.Modules.Base.Interface.Models;

public class ChatSubmitEventArgs(string message) : EventArgs
{
    public string Message { get; set; } = message;
}
