namespace Vitarish.Sea.Interface.Components.Procedures;

public partial class ProcedureCreateDialog : ProceduresBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter] public Guid ProjectId { get; set; }
    [Parameter] public string ProjectName { get; set; } = string.Empty;

    private string name = string.Empty;
    private ProcedureDto? templateProcedure;

    [Inject] private IAttachmentsService AttachmentsService { get; set; } = default!;
    [Inject] private IEventRolesService EventRolesService { get; set; } = default!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            await PromptTemplateAsync();
        }
    }

    private async Task PromptTemplateAsync() => await SelectTemplateInternalAsync();

    private async Task SelectTemplateAsync() => await SelectTemplateInternalAsync();

    private async Task SelectTemplateInternalAsync()
    {
        IDialogReference dialog = await DialogService.ShowAsync<ProcedureTemplatesBrowser>(Localizer["TemplateItem", Localizer["Procedure"]]);
        DialogResult? result = await dialog.Result;
        if (result?.Canceled == false && result.Data is ProcedureDto procedure)
        {
            templateProcedure = procedure;
            name = procedure.Name;
        }

        StateHasChanged();
    }

    private async Task SaveAsync()
    {
        ProcedureDescriptorDto descriptor = new()
        {
            ProjectId = ProjectId,
            Search = name
        };

        IReadOnlyList<ProcedureDto> existing = await ProceduresService.GetListAsync(descriptor, CancellationToken.None)
            .EnsureValueAsync("Load procedures");
        if (existing.Any(p => string.Equals(p.Name, name, StringComparison.CurrentCultureIgnoreCase)))
        {
            _ = await DialogService.ShowMessageBox(
                Localizer["Procedure"],
                Localizer["ProcedureExistsError", ProjectName, name],
                yesText: Localizer["Ok"]);
            return;
        }

        Guid id = await ProceduresService.AddAsync(ProjectId, name, CancellationToken.None);
        if (templateProcedure is not null)
        {
            await CopyEventsAsync(templateProcedure.Id, id);
        }

        MudDialog.Close(DialogResult.Ok(true));
    }

    private async Task CopyEventsAsync(Guid fromProcedureId, Guid toProcedureId)
    {
        IReadOnlyList<EventDto> events = (await EventsService.GetByProcedureAsync(fromProcedureId, CancellationToken.None)).SortByChain();
        Guid? previous = null;
        foreach (EventDto e in events)
        {
            EventDto? added = await EventsService.AddAsync(ProjectId, toProcedureId, previous, CancellationToken.None);
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

            IReadOnlyList<AttachmentDto> attachments = await AttachmentsService.GetByEventAsync(e.Id, CancellationToken.None);
            if (attachments.Count > 0)
            {
                await AttachmentsService.AddAsync(added.Id, attachments, CancellationToken.None);
            }

            previous = added.Id;
        }
    }

    private void Cancel() => MudDialog.Cancel();
}
