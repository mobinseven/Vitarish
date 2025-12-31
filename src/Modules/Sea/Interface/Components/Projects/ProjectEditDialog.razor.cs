namespace Vitarish.Sea.Interface.Components.Projects;

public partial class ProjectEditDialog : ProjectsBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter] public ProjectDto? Project { get; set; }

    private string name = string.Empty;
    private string description = string.Empty;
    private bool template;
    private string metadata = "{}";

    protected override void OnInitialized()
    {
        if (Project is not null)
        {
            name = Project.Name;
            description = Project.Description;
            template = Project.Template;
            metadata = Project.Metadata;
        }
    }

    private async Task SaveAsync()
    {
        if (Project is null)
        {
            _ = await ProjectsService.AddAsync(name, description, template, metadata, CancellationToken.None);
        }
        else
        {
            await ProjectsService.UpdateAsync(Project.Id, name, description, template, metadata, CancellationToken.None);
        }
        MudDialog.Close(DialogResult.Ok(true));
    }

    private void Cancel() => MudDialog.Cancel();
}
