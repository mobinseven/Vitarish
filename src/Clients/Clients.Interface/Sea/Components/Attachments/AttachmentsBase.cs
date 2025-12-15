namespace Vitarish.Clients.Interface.Sea.Components.Attachments;

public class AttachmentsBase : SeaCommonBase
{
    [Inject] protected IAttachmentsClient AttachmentsService { get; set; } = default!;
    protected override void OnInitialized()
    {
        base.OnInitialized();
        RenderForType<AttachmentDto>();
    }
}
