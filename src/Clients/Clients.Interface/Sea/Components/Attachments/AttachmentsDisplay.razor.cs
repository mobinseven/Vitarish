namespace Vitarish.Clients.Interface.Sea.Components.Attachments;

public partial class AttachmentsDisplay : SeaCommonBase
{
    [Parameter] public Color Color { get; set; }
    [Parameter, EditorRequired] public EventDto Event { get; set; }
    [Parameter, EditorRequired] public AttachmentDto Attachment { get; set; }

    private async Task ShowDetails()
    {
        DialogParameters parameters = new()
        {
            { nameof(AttachmentDetailsDialog.Attachment), Attachment },
            {nameof(AttachmentDetailsDialog.Event), Event },
            { nameof(AttachmentDetailsDialog.Color), Color }
        };

        _ = await DialogService.ShowAsync<AttachmentDetailsDialog>(Localizer["Attachment"], parameters);
    }

    private string Icon(string contentType) => contentType.StartsWith("image")
            ? "fa-thin fa-file-image"
            : contentType.StartsWith("video") ? "fa-thin fa-file-video" : "fa-thin fa-file-lines";
}
