namespace Vitarish.Clients.Interface.Sea.Components.Projects;

public partial class ProjectTemplateEditDialog : ProjectsBase
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter]
    public ProjectDto? Project { get; set; }

    private string name = string.Empty;
    private string description = string.Empty;
    private string metadata = "{}";

    protected override void OnInitialized()
    {
        if (Project is not null)
        {
            name = Project.Name;
            description = Project.Description;
            metadata = Project.Metadata;
        }
    }

    private async Task SaveAsync()
    {
        if (Project is null)
        {
            _ = await ProjectsService.CreateProjectAsync(name, description, template: true, metadata, CancellationToken.None);
        }
        else
        {
            await ProjectsService.UpdateProjectDetailsAsync(Project.Id, name, description, template: true, metadata, CancellationToken.None);
        }

        MudDialog.Close(DialogResult.Ok(true));
    }

    private void Cancel()
        => MudDialog.Cancel();
}
