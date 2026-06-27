namespace Vitarish.Sea.Interface.Components.Procedures;

public partial class ProcedureCreateDialog : ProceduresBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter] public Guid ProjectId { get; set; }
    [Parameter] public string ProjectName { get; set; } = string.Empty;

    private readonly ProcedureWriteDto ProcedureWriteDto = new();

    private ProcedureReadDto? templateProcedure
    {
        get;
        set
        {
            field = value;
            if (value is null)
            {
                return;
            }
            ProcedureWriteDto.Name = value.Name;
        }
    }

    
    private async Task SaveAsync()
    {
        ProcedureDescriptorDto descriptor = new()
        {
            ProjectId = ProjectId,
            Search = ProcedureWriteDto.Name
        };

        IReadOnlyList<ProcedureReadDto> existing = await ValueOrNotifyAsync(SeaClient.ListProceduresAsync(ProjectId, CancellationToken.None)) ?? [];
        if (existing.Any(p => string.Equals(p.Name, ProcedureWriteDto.Name, StringComparison.CurrentCultureIgnoreCase)))
        {
            _ = await DialogService.ShowMessageBoxAsync(
                Localizer["Procedure"],
                Localizer["ProcedureExistsError", ProjectName, ProcedureWriteDto.Name],
                yesText: Localizer["Ok"]);
            return;
        }

        SeaCommandResult? createResult = await ValueOrNotifyAsync(SeaClient.CreateProcedureAsync(new ProcedureWriteDto { ProjectId = ProjectId, Name = ProcedureWriteDto.Name }, CancellationToken.None));
        if (createResult?.EntityId is not Guid addedId) return;
        ProcedureReadDto? added = await ValueOrNotifyAsync(SeaClient.GetProcedureAsync(addedId, CancellationToken.None));
        if (added is null) return;
        if (templateProcedure is not null)
        {
            await CopyEventsAsync(templateProcedure.Id, added.Id);
        }

        MudDialog.Close(DialogResult.Ok(added.Id));
    }

    private async Task CopyEventsAsync(Guid fromProcedureId, Guid toProcedureId)
    {
        IReadOnlyList<EventReadDto> events = (await ValueOrNotifyAsync(SeaClient.ListEventsByProcedureAsync(fromProcedureId, CancellationToken.None)) ?? []).SortByChain();
        Guid? previous = null;
        foreach (EventReadDto e in events)
        {
            SeaCommandResult? createResult = await ValueOrNotifyAsync(SeaClient.CreateEventAfterAsync(previous, new EventWriteDto { ProjectId = ProjectId, ProcedureId = toProcedureId, PreviousEventId = previous }, CancellationToken.None));
            EventReadDto? added = createResult?.EntityId is Guid addedId ? await ValueOrNotifyAsync(SeaClient.GetEventAsync(addedId, CancellationToken.None)) : null;
            if (added is null)
            {
                continue;
            }

            _ = await CommandOrNotifyAsync(SeaClient.UpdateEventAsync(new EventWriteDto { Id = added.Id, ProjectId = ProjectId, ProcedureId = toProcedureId, Title = e.Title, Description = e.Description }, CancellationToken.None));

            (IReadOnlyList<Guid> ReadRoleIds, IReadOnlyList<Guid> WriteRoleIds) = (await ValueOrNotifyAsync(SeaClient.GetEventReadRoleIdsAsync(e.Id, CancellationToken.None)) ?? [], await ValueOrNotifyAsync(SeaClient.GetEventWriteRoleIdsAsync(e.Id, CancellationToken.None)) ?? []);
            foreach (Guid readRole in ReadRoleIds)
            {
                await CommandOrNotifyAsync(SeaClient.AssignEventRoleAsync(new EventRoleWriteDto(added.Id, readRole, RoleType.Read), CancellationToken.None));
            }

            foreach (Guid writeRole in WriteRoleIds)
            {
                await CommandOrNotifyAsync(SeaClient.AssignEventRoleAsync(new EventRoleWriteDto(added.Id, writeRole, RoleType.Write), CancellationToken.None));
            }

            IReadOnlyList<AttachmentReadDto> attachments = await ValueOrNotifyAsync(SeaClient.ListAttachmentsByEventAsync(e.Id, CancellationToken.None)) ?? [];
            if (attachments.Count > 0)
            {
                await CommandOrNotifyAsync(SeaClient.AddAttachmentsAsync([.. attachments.Select(a => new AttachmentWriteDto(added.Id, a.Content, a.ContentType, a.ContentThumbnail, a.Size) { Id = a.Id })], CancellationToken.None));
            }

            previous = added.Id;
        }
    }

    private void Cancel() => MudDialog.Cancel();
}
