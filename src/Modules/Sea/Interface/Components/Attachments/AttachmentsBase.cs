namespace Vitarish.Sea.Interface.Components.Attachments;

public class AttachmentsBase : SeaCommonBase
{
    [Inject] protected IAttachmentsService AttachmentsService { get; set; } = default!;
    protected override void OnInitialized()
    {
        base.OnInitialized();
        RenderForType<AttachmentDto>();
    }
}
