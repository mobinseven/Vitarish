namespace Vitarish.Sea.Interface.Components.Attachments;

public partial class AttachmentCreator : AttachmentsBase
{
    private string? _value = string.Empty;

    [Parameter, EditorRequired] public ProjectReadDto Project { get; set; } = default!;
    [Parameter, EditorRequired] public ProcedureReadDto Procedure { get; set; } = default!;
    [Parameter, EditorRequired] public EventReadDto Event { get; set; } = default!;
    [Parameter] public RenderFragment? Actions { get; set; }

    private async Task OnUploaded(FileInformationDto fileInfo)
    {
        AttachmentReadDto attachment = new(
            fileInfo.Id.ToString(),
            fileInfo.ContentType ?? string.Empty,
            fileInfo.Thumbnail,
            fileInfo.Metadata.Size);
        await AttachmentsService.AddAsync(Event.Id, [attachment], CancellationToken.None);
    }
    private async Task AddTextAttachment()
    {
        if (string.IsNullOrWhiteSpace(_value))
        {
            return;
        }

        _ = await AttachmentsService.AddAsync(Event.Id, _value,
            "text/plain",
            null,
            Encoding.UTF8.GetByteCount(_value),
            CancellationToken.None) ?? throw new Exception("Attachment was not added");

        _value = string.Empty;
    }
}
