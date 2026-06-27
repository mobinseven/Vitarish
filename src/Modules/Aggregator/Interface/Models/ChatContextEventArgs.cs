namespace Vitarish.Modules.Aggregator.Interface.Models;

public class ChatContextEventArgs(RenderFragment renderFragment) : EventArgs
{
    public RenderFragment ChatContext { get; set; } = renderFragment;
}
