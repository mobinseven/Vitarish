using System.Text;
using Vitarish.Clients.Modules.Files.Models;

namespace Vitarish.Clients.Interface.Sea.Components.Attachments;

public partial class AttachmentCreator : AttachmentsBase
{
    private string? _value = string.Empty;

    [Parameter, EditorRequired] public ProjectDto Project { get; set; } = default!;
    [Parameter, EditorRequired] public ProcedureDto Procedure { get; set; } = default!;
    [Parameter, EditorRequired] public EventDto Event { get; set; } = default!;
    [Parameter] public RenderFragment? Actions { get; set; }

    private async Task OnUploaded(FileInformationDto fileInfo)
    {
        AttachmentDto attachment = new(
            fileInfo.Url ?? string.Empty,
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
