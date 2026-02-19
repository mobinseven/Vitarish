namespace Vitarish.Sea.Interface.Components.Attachments;

public partial class AttachmentsDisplay : SeaCommonBase
{
    [Parameter] public Color Color { get; set; }
    [Parameter, EditorRequired] public EventReadDto Event { get; set; }
    [Parameter, EditorRequired] public AttachmentReadDto Attachment { get; set; }

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

    private static string Icon(string contentType) => contentType.StartsWith("image")
            ? "fa-regular fa-file-image"
            : contentType.StartsWith("video") ? "fa-regular fa-file-video" : "fa-regular fa-file-lines";
}
