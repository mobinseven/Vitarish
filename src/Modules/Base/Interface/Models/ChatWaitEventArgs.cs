namespace Vitarish.Modules.Base.Interface.Models;

public class ChatWaitEventArgs(bool waiting) : EventArgs
{
    public bool Waiting { get; set; } = waiting;
}
