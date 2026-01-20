using Vitarish.Sea.Interface.Plugins.Clinic.Models;

namespace Vitarish.Sea.Interface.Plugins.Clinic.Projects;

public partial class ClinicProjectsGrid
{
    private List<ProjectDto>? projectsViaSurgeryData;
    private List<ProjectDto>? projectsLackSurgeryData;

    private enum SortBy
    {
        SurgeryDate,
        SurgeryDateDesc,
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await ReloadData();
        }
    }

    protected override async Task ReloadData()
    {
        ProjectDescriptorDto descriptor = new()
        {
            Skip = 0,
            Take = int.MaxValue,
            Search = SearchString,
            Template = TemplateMode
        };

        IReadOnlyList<ProjectDto> Items = await ProjectsService.GetListAsync(descriptor, CancellationToken.None)
            .EnsureValueAsync("Load projects");

        projects = [.. Items];
        projects.ForEach(p => p.MetadataObject = p.GetClinicProjectMetadata());

        projectsViaSurgeryData = [.. projects.Where(project => ((ClinicProjectMetadata?)project.MetadataObject)?.DateOfSurgery.HasValue ?? false)];
        projectsLackSurgeryData = [.. projects.Where(project => !((ClinicProjectMetadata?)project.MetadataObject)?.DateOfSurgery.HasValue ?? true)];

        Sort();
    }

    private async void Sort()
    {
        if (UsageConfigurationMetadata?.ProjectsSortMethod == SortBy.SurgeryDateDesc.ToString())
        {
            projectsViaSurgeryData = [.. projectsViaSurgeryData!
            .OrderByDescending(e => ((ClinicProjectMetadata?)e.MetadataObject)?.DateOfSurgery)
            .ThenByDescending(e => (e.Stats?.NewItems ?? 0) > 0)
            .ThenByDescending(e => e.Stats?.Unread ?? false)
            .ThenBy(e => e.Name)];
        }
        else if (UsageConfigurationMetadata?.ProjectsSortMethod == SortBy.SurgeryDate.ToString())
        {
            projectsViaSurgeryData = [.. projectsViaSurgeryData!
            .OrderBy(e=> ((ClinicProjectMetadata?)e.MetadataObject)?.DateOfSurgery)
            .ThenByDescending(e => (e.Stats?.NewItems ?? 0) > 0)
            .ThenByDescending(e => e.Stats?.Unread ?? false)
            .ThenBy(e => e.Name)];
        }
        projectsLackSurgeryData = [.. projectsLackSurgeryData!
            .OrderByDescending(e => (e.Stats?.NewItems ?? 0) > 0)
            .ThenByDescending(e => e.Stats?.Unread ?? false)
            .ThenBy(e => e.Name)];
        await InvokeAsync(StateHasChanged);
    }

    protected override async Task ReloadRequired() => await ReloadData();
}
