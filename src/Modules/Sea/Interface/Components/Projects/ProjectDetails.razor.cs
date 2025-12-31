namespace Vitarish.Sea.Interface.Components.Projects;

public partial class ProjectDetails : ProjectsBase
{
    [Inject] private IEnumerable<IProcedureCreationFormPlugin> ProcedureCreationFormPlugins { get; set; } = default!;
    [Inject] private IServiceProvider ServiceProvider { get; set; } = default!;

    [Parameter, EditorRequired] public ProjectDto Project { get; set; } = default!;
    [Parameter] public EventCallback<ProjectDto> OnEdit { get; set; }
    [Parameter] public EventCallback<ProjectDto> OnDelete { get; set; }
    [Parameter] public EventCallback<ProjectDto> OnAddProcedure { get; set; }


    private async Task ShowPluginAsync(IProcedureCreationFormPlugin plugin, ProcedureCreationFormContext context)
    {
        DialogParameters parameters = new()
        {
            { nameof(PluginDialog<ProcedureCreationFormContext>.Plugin), plugin },
            { nameof(PluginDialog<ProcedureCreationFormContext>.Context), context }
        };

        _ = await DialogService.ShowAsync<PluginDialog<ProcedureCreationFormContext>>(plugin.DisplayName, parameters);
    }

    private ProcedureCreationFormContext BuildCreationFormContext()
        => new(ServiceProvider, Project, new Dictionary<string, object?>
        {
            { "ProjectId", Project.Id }
        });

    private async Task AddProcedure()
    {
        DialogParameters parameters = new()
        {
            { nameof(ProcedureCreateDialog.ProjectId), Project.Id },
            { nameof(ProcedureCreateDialog.ProjectName), Project.Name }
        };

        ProcedureCreationFormContext context = BuildCreationFormContext();
        List<IProcedureCreationFormPlugin> plugins =
            [.. ProcedureCreationFormPlugins
                .Where(plugin => plugin.IsAvailable(context))
                .OrderBy(plugin => plugin.DisplayName, StringComparer.CurrentCultureIgnoreCase)];

        if (plugins.Count == 0)
        {
            _ = await DialogService.ShowAsync<ProcedureCreateDialog>(Localizer["Procedure"], parameters);
            return;
        }

        if (plugins.Count == 1)
        {
            await ShowPluginAsync(plugins[0], context);
            return;
        }

        DialogParameters selectorParameters = new()
        {
            { nameof(ProcedureCreationFormSelectorDialog.Options), plugins },
            { nameof(ProcedureCreationFormSelectorDialog.ShowStandardForm), true }
        };

        IDialogReference selector = await DialogService.ShowAsync<ProcedureCreationFormSelectorDialog>(Localizer["Procedure"], selectorParameters);
        DialogResult? result = await selector.Result;

        if (result is null || result.Canceled)
        {
            return;
        }

        if (result.Data is IProcedureCreationFormPlugin plugin)
        {
            await ShowPluginAsync(plugin, context);
            return;
        }

        _ = await DialogService.ShowAsync<ProcedureCreateDialog>(Localizer["Procedure"], parameters);
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
        await OnEdit.InvokeAsync(Project);
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
            await OnDelete.InvokeAsync(project);
        }
    }
}

