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

    [Inject] private IAttachmentsService AttachmentsService { get; set; } = default!;
    [Inject] private IEventRolesService EventRolesService { get; set; } = default!;

    private async Task SaveAsync()
    {
        ProcedureDescriptorDto descriptor = new()
        {
            ProjectId = ProjectId,
            Search = ProcedureWriteDto.Name
        };

        IReadOnlyList<ProcedureReadDto> existing = await ProceduresService.GetListAsync(descriptor, CancellationToken.None).GetValueOrDefaultAsync() ?? [];
        if (existing.Any(p => string.Equals(p.Name, ProcedureWriteDto.Name, StringComparison.CurrentCultureIgnoreCase)))
        {
            _ = await DialogService.ShowMessageBox(
                Localizer["Procedure"],
                Localizer["ProcedureExistsError", ProjectName, ProcedureWriteDto.Name],
                yesText: Localizer["Ok"]);
            return;
        }

        ProcedureReadDto added = await ProceduresService.AddAsync(new ProcedureWriteDto { ProjectId = ProjectId, Name = ProcedureWriteDto.Name }, CancellationToken.None).EnsureValueAsync();
        if (templateProcedure is not null)
        {
            await CopyEventsAsync(templateProcedure.Id, added.Id);
        }

        MudDialog.Close(DialogResult.Ok(added.Id));
    }

    private async Task CopyEventsAsync(Guid fromProcedureId, Guid toProcedureId)
    {
        IReadOnlyList<EventReadDto> events = (await EventsService.GetByProcedureAsync(fromProcedureId, CancellationToken.None)).SortByChain();
        Guid? previous = null;
        foreach (EventReadDto e in events)
        {
            EventReadDto? added = await EventsService.AddAsync(ProjectId, toProcedureId, previous, CancellationToken.None);
            if (added is null)
            {
                continue;
            }

            _ = await EventsService.UpdateAsync(added.Id, e.Title, e.Description, CancellationToken.None);

            (IReadOnlyList<Guid> ReadRoleIds, IReadOnlyList<Guid> WriteRoleIds) = await EventRolesService.GetIdsAsync(e.Id, CancellationToken.None);
            foreach (Guid readRole in ReadRoleIds)
            {
                await EventRolesService.AssignAsync(added.Id, readRole, false, CancellationToken.None);
            }

            foreach (Guid writeRole in WriteRoleIds)
            {
                await EventRolesService.AssignAsync(added.Id, writeRole, true, CancellationToken.None);
            }

            IReadOnlyList<AttachmentReadDto> attachments = await AttachmentsService.GetByEventAsync(e.Id, CancellationToken.None);
            if (attachments.Count > 0)
            {
                await AttachmentsService.AddAsync(added.Id, attachments, CancellationToken.None);
            }

            previous = added.Id;
        }
    }

    private void Cancel() => MudDialog.Cancel();
}
