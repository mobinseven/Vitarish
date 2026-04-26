namespace Vitarish.Sea.Interface.Components.Attachments;

public partial class AttachmentCreator : AttachmentsBase
{
    private string? _value = string.Empty;

    [Parameter, EditorRequired] public ProjectReadDto Project { get; set; } = default!;
    [Parameter, EditorRequired] public ProcedureReadDto Procedure { get; set; } = default!;
    [Parameter, EditorRequired] public EventReadDto Event { get; set; } = default!;
    [Parameter] public RenderFragment? Actions { get; set; }
    [Parameter] public EventCallback OnCreated { get; set; }

    private async Task OnUploaded(FileInformationReadDto fileInfo)
    {
        AttachmentReadDto attachment = new()
        {
            Id = Guid.Parse(fileInfo.Id.ToString()),
            Content = fileInfo.Id.ToString(),
            ContentType = fileInfo.ContentType ?? string.Empty,
            ContentThumbnail = fileInfo.Thumbnail,
            Size = fileInfo.Metadata.Size
        };
        await AttachmentsService.AddAsync(Event.Id, [attachment], CancellationToken.None);
        await OnCreated.InvokeAsync();
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
        await OnCreated.InvokeAsync();
    }
}
