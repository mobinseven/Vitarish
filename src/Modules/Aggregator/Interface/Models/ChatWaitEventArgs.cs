namespace Vitarish.Modules.Aggregator.Interface.Models;

public class ChatWaitEventArgs(bool waiting) : EventArgs
{
    public bool Waiting { get; set; } = waiting;
}
