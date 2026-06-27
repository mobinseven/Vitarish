namespace Vitarish.Sea.Interface.Components.Base;

public class AttachmentsBase : SeaCommonBase
{
    protected override void OnInitialized()
    {
        base.OnInitialized();
        RenderForType<AttachmentReadDto>();
    }
}
