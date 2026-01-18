namespace Vitarish.Sea.Interface.Components.Projects;

public partial class ProjectDetails : ProjectsBase
{
    [Parameter, EditorRequired] public ProjectDto Project { get; set; } = default!;
    [Parameter] public EventCallback OnDelete { get; set; }
    [Parameter] public EventCallback<Guid> OnProcedureAdded { get; set; }
    [Inject] private IEnumerable<IProjectDetailsPlugin> Plugins { get; set; } = [];
    private IProjectDetailsPlugin? Plugin => Plugins.SingleOrDefault(p => p.Id == UsageConfigurationMetadata?.PluginIdentifier);
    protected virtual async Task AddProcedure()
    {
        DialogParameters parameters = new()
        {
            { nameof(ProcedureCreateDialog.ProjectId), Project.Id },
            { nameof(ProcedureCreateDialog.ProjectName), Project.Name }
        };

        var dialog = await DialogService.ShowAsync<ProcedureCreateDialog>(Localizer["Procedure"], parameters);

        var result = await dialog.Result;

        if(result?.Canceled ?? true)
        {
            return;
        }

        if(result.Data is Guid procedureId)
        {
            await OnProcedureAdded.InvokeAsync(procedureId);
        }
    }

    protected virtual async Task EditProject(ProjectDto project)
    {
        DialogParameters parameters = new()
        {
            { nameof(ProjectEditDialog.Project), project }
        };
        IDialogReference dialog = await DialogService.ShowAsync<ProjectEditDialog>(null, parameters);

        DialogResult? result = await dialog.Result;

        if (result is null)
        {
            return;
        }

        if (result.Canceled)
        {
            return;
        }
    }

    protected virtual async Task DeleteProject(ProjectDto project)
    {
        DialogParameters parameters = new()
        {
            { nameof(StrictConfirmDialog.ContentText), Localizer["StrictConfirmationQuestion", Localizer["Project"]].Value },
            { nameof(StrictConfirmDialog.ItemName), project.Name },
            { nameof(StrictConfirmDialog.ButtonText), Localizer["Delete", Localizer["Project"]].Value },
            { nameof(StrictConfirmDialog.Color), Color.Error }
        };
        IDialogReference dialog = await DialogService.ShowAsync<StrictConfirmDialog>(Localizer["Delete", Localizer["Project"]], parameters);
        DialogResult? result = await dialog.Result;

        if (result is null)
        {
            return;
        }

        if (result.Canceled)
        {
            return;
        }
        else
        {
            await ProjectsService.RemoveAsync(project.Id, CancellationToken.None).EnsureSuccessAsync("Remove project");
            await OnDelete.InvokeAsync();
        }
    }
}

