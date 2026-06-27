namespace Vitarish.Sea.Interface.Components.Projects;

public partial class ProjectEditForm : ProjectsBase
{
    [Inject] private IEnumerable<IProjectEditPlugin> ProjectEditPlugins { get; set; } = [];

    [Parameter] public ProjectReadDto? Project { get; set; }
    [Parameter] public EventCallback<Guid> OnUpdate { get; set; }
    [Parameter] public EventCallback<Guid> OnCreate { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }
    [Parameter] public bool TemplateMode { get; set; } = false;

    private ProjectWriteDto projectWriteDto = new();
    private IProjectEditPlugin? Plugin => ProjectEditPlugins.SingleOrDefault(p => p.Id == TenantService?.PluginIdentifier);


    protected override void OnInitialized()
    {
        if (Project is not null)
        {
            projectWriteDto = Project.ToUpdateRequest();
        }
    }
    protected virtual async Task SaveAsync()
    {
        if (Project is null)
        {
            SeaCommandResult? created = await ValueOrNotifyAsync(SeaClient.CreateProjectAsync(projectWriteDto, CancellationToken.None));
            if (created?.EntityId is Guid createdId)
            {
                await OnCreate.InvokeAsync(createdId);
            }
        }
        else
        {
            _ = await CommandOrNotifyAsync(SeaClient.UpdateProjectAsync(projectWriteDto, CancellationToken.None));
            await OnUpdate.InvokeAsync(Project.Id);
        }
    }

    protected Task OnAvatarUploaded(FileInformationDto fileInfo)
    {
        projectWriteDto.AvatarFileId = fileInfo.Id;
        return InvokeAsync(StateHasChanged);
    }

    protected Task RemoveAvatar()
    {
        projectWriteDto.AvatarFileId = null;
        return InvokeAsync(StateHasChanged);
    }

    protected void Cancel() => OnCancel.InvokeAsync();
}
