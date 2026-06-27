namespace Vitarish.Sea.Interface.Components.Attachments;

public partial class AttachmentCreator : AttachmentsBase
{
    private static AttachmentWriteDto ToWrite(AttachmentReadDto attachment, Guid eventId) => new(eventId, attachment.Content, attachment.ContentType, attachment.ContentThumbnail, attachment.Size) { Id = attachment.Id };
    private string? _value = string.Empty;

    [Parameter, EditorRequired] public ProjectReadDto Project { get; set; } = default!;
    [Parameter, EditorRequired] public ProcedureReadDto Procedure { get; set; } = default!;
    [Parameter, EditorRequired] public EventReadDto Event { get; set; } = default!;
    [Parameter] public RenderFragment? Actions { get; set; }
    [Parameter] public EventCallback OnCreated { get; set; }

    private async Task OnUploaded(FileInformationDto fileInfo)
    {
        AttachmentReadDto attachment = new()
        {
            Id = Guid.Parse(fileInfo.Id.ToString()),
            Content = fileInfo.Id.ToString(),
            ContentType = fileInfo.ContentType ?? string.Empty,
            ContentThumbnail = fileInfo.Thumbnail,
            Size = fileInfo.Metadata.Size
        };
        await CommandOrNotifyAsync(SeaClient.AddAttachmentsAsync([ToWrite(attachment, Event.Id)], CancellationToken.None));
        await OnCreated.InvokeAsync();
    }
    private async Task AddTextAttachment()
    {
        if (string.IsNullOrWhiteSpace(_value))
        {
            return;
        }

        await CommandOrNotifyAsync(SeaClient.AddTextAttachmentMetadataAsync(new AttachmentWriteDto(Event.Id, _value, "text/plain", null, Encoding.UTF8.GetByteCount(_value)), CancellationToken.None));

        _value = string.Empty;
        await OnCreated.InvokeAsync();
    }
}
