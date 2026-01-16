namespace Vitarish.Sea.Interface.Components.Projects;

public partial class ProjectDetails : ProjectsBase
{
    [Parameter, EditorRequired] public ProjectDto Project { get; set; } = default!;
    [Parameter] public EventCallback OnEdit { get; set; }
    [Parameter] public EventCallback OnDelete { get; set; }
    [Parameter] public EventCallback<Guid> OnProcedureAdded { get; set; }

    private async Task AddProcedure()
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

    private async Task EditProject(ProjectDto project)
    {
        DialogParameters parameters = new()
        {
            { nameof(ProjectEditDialog.Project), project }
        };
        IDialogReference dialog = await DialogService.ShowAsync<ProjectEditDialog>(Localizer["Project"], parameters);

        DialogResult? result = await dialog.Result;

        if (result is null)
        {
            return;
        }

        if (result.Canceled)
        {
            return;
        }

        Project = await ProjectsService.GetAsync(Project.Id, CancellationToken.None)
            .GetValueOrDefaultAsync() ?? throw new Exception("Project was null");
        await OnEdit.InvokeAsync();
    }

    private async Task DeleteProject(ProjectDto project)
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

