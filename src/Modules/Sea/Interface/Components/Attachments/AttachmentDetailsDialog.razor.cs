namespace Vitarish.Sea.Interface.Components.Attachments;

public partial class AttachmentDetailsDialog : SeaCommonBase
{
            
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter, EditorRequired] public EventReadDto Event { get; set; } = default!;
    [Parameter, EditorRequired] public AttachmentReadDto Attachment { get; set; } = default!;

    [Parameter] public Color Color { get; set; }

    private Guid FileId => Guid.TryParse(Attachment.Content, out Guid id) ? id : Guid.Empty;

    private bool CanEdit { get; set; }
    private bool CanDelete { get; set; }
    private bool editing;
    private string editContent = string.Empty;
    private Guid[] readers = [];

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await LoadReadersAsync();
        CanEdit = (await ValueOrNotifyAsync(SeaClient.CanWriteEventAsync(Event.Id, CancellationToken.None))) && Event.CommittedBy.ToString() == RequestContext.UserId;
        CanDelete = (await ValueOrNotifyAsync(SeaClient.CanWriteEventAsync(Event.Id, CancellationToken.None)));
        StateHasChanged();
    }

    private async Task LoadReadersAsync() => readers = [.. (await ValueOrNotifyAsync(SeaClient.ListReadersAsync(Attachment.Id, CancellationToken.None)) ?? []).Select(r => r.ObjectId).Except([Guid.Parse(Attachment.CreatedBy ?? Guid.Empty.ToString())])];

    private void Close() => MudDialog.Close();

    private void StartEdit()
    {
        editContent = Attachment.Content;
        editing = true;
    }

    private void CancelEdit() => editing = false;

    private async Task SaveAsync()
    {
        bool saved = await CommandOrNotifyAsync(SeaClient.UpdateTextAttachmentAsync(new AttachmentWriteDto(Event.Id, editContent, Attachment.ContentType, Attachment.ContentThumbnail, Attachment.Size) { Id = Attachment.Id }, CancellationToken.None));
        AttachmentReadDto? updated = saved ? Attachment with { Content = editContent } : null;

        if (updated is not null)
        {
            Attachment = updated;
        }

        editing = false;
    }

    private string FormatReader(Guid readerId)
    {
        string? name = GetUserFullNameById(readerId);
        return string.IsNullOrWhiteSpace(name) ? readerId.ToString() : name;
    }

    private string[] GetReaders()
    {
        if (readers.Length == 0)
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
        await CommandOrNotifyAsync(SeaClient.RemoveAttachmentAsync(Attachment.Id, CancellationToken.None));
        MudDialog.Close();
    }
}
