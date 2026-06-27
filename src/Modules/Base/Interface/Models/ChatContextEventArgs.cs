namespace Vitarish.Modules.Base.Interface.Models;

public class ChatContextEventArgs(RenderFragment renderFragment) : EventArgs
{
    public RenderFragment ChatContext { get; set; } = renderFragment;
}
