namespace Vitarish.Sea.Interface.Components.Projects;

public partial class ProjectDetails : ProjectsBase
{
    [Parameter, EditorRequired] public ProjectReadDto Project { get; set; } = default!;
    [Parameter] public EventCallback OnDeleted { get; set; }
    [Parameter] public EventCallback<Guid> OnEdit { get; set; }
    [Inject] private IEnumerable<IProjectDetailsPlugin> Plugins { get; set; } = [];
    private IProjectDetailsPlugin? Plugin => Plugins.SingleOrDefault(p => p.Id == MetadataService?.PluginIdentifier);

    protected virtual Task EditProject() => OnEdit.InvokeAsync(Project.Id);

    protected virtual async Task DeleteProject()
    {
        DialogParameters parameters = new()
        {
            { nameof(StrictConfirmDialog.ContentText), Localizer["StrictConfirmationQuestion", Localizer["Project"]].Value },
            { nameof(StrictConfirmDialog.ItemName), Project.Name },
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
            await ProjectsService.RemoveAsync(Project.Id, CancellationToken.None);
            await OnDeleted.InvokeAsync();
        }
    }
}

