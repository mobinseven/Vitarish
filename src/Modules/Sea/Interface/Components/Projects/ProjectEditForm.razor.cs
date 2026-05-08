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
            projectWriteDto = Project.ToWrite();
        }
    }
    protected virtual async Task SaveAsync()
    {
        if (Project is null)
        {
            ProjectReadDto created = await ProjectsService.AddAsync(projectWriteDto, CancellationToken.None).EnsureValueAsync();
            await OnCreate.InvokeAsync(created.Id);
        }
        else
        {
            _ = await ProjectsService.UpdateAsync(projectWriteDto, CancellationToken.None);
            await OnUpdate.InvokeAsync(Project.Id);
        }
    }

    protected Task OnAvatarUploaded(FileInformationReadDto fileInfo)
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
