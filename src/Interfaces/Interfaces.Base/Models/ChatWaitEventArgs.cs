namespace Vitarish.Interfaces.Base.Models;

public class ChatWaitEventArgs(bool waiting) : EventArgs
{
    public bool Waiting { get; set; } = waiting;
}
