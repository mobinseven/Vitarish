namespace Vitarish.Interfaces.Base.Models;

public class ChatContextEventArgs(RenderFragment renderFragment) : EventArgs
{
    public RenderFragment ChatContext { get; set; } = renderFragment;
}
