using Vitarish.Users.Domain.Entities;

namespace Vitarish.Sea.Interface.Components.Attachments;

public partial class AttachmentDetailsDialog : SeaCommonBase
{
    [Inject] private IAccessControlledEventsService EventsService { get; set; } = default!;
    [Inject] private IAttachmentsService AttachmentsService { get; set; } = default!;
    [Inject] private IReceiptsService ReceiptsService { get; set; } = default!;

    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter, EditorRequired] public EventDto Event { get; set; } = default!;
    [Parameter, EditorRequired] public AttachmentDto Attachment { get; set; } = default!;

    [Parameter] public Color Color { get; set; }

    private bool CanWrite { get; set; }
    private bool editing;
    private string editContent = string.Empty;
    private IReadOnlyList<Guid> readers = [];

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await LoadReadersAsync();
        CanWrite = await EventsService.CanWriteEvent(Event.Id);
        StateHasChanged();
    }

    private async Task LoadReadersAsync() => readers = await ReceiptsService.GetReadersAsync(Attachment.Id, CancellationToken.None) ?? Array.Empty<Guid>();

    private void Close() => MudDialog.Close();

    private void StartEdit()
    {
        editContent = Attachment.Content;
        editing = true;
    }

    private void CancelEdit() => editing = false;

    private async Task SaveAsync()
    {
        AttachmentDto? updated = await AttachmentsService.UpdateAsync(
            Event.Id,
            Attachment.Id,
            editContent,
            CancellationToken.None);
        if (updated is not null)
        {
            Attachment = updated;
        }

        editing = false;
    }

    private string FormatReader(Guid readerId)
    {
        string? name = Users.FirstOrDefault(x => x.Id == readerId.ToString())?.FullName;
        return string.IsNullOrWhiteSpace(name) ? readerId.ToString() : name;
    }

    private string[] GetReaders()
    {
        if (readers.Count == 0)
        {
            return [];
        }

        string[] names = [.. readers
            .Select(FormatReader)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)];

        return names;
    }
    private async Task DeleteAttachment()
    {
        DialogParameters parameters = new()
        {
            { nameof(ConfirmDialog.ContentText), Localizer["ConfirmationQuestion"].Value },
            { nameof(ConfirmDialog.ButtonText), Localizer["Delete"].Value },
            { nameof(ConfirmDialog.Color), Color.Error }
        };
        IDialogReference dialog = await DialogService.ShowAsync<ConfirmDialog>(Localizer["Delete"], parameters);
        DialogResult? result = await dialog.Result;
        if (result is null || result.Canceled)
        {
            return;
        }
        await AttachmentsService.RemoveAsync(Event.Id, Attachment.Id, CancellationToken.None);
        MudDialog.Close();
    }
}
