using Vitarish.Interfaces.Base.Models;
using Vitarish.Sea.Interface.Plugins.Clinic.Models;

namespace Vitarish.Sea.Interface.Plugins.Clinic.Projects;

public partial class ClinicProjectsGrid
{
    private List<ProjectDto>? projectsViaSurgeryData;
    private List<ProjectDto>? projectsLackSurgeryData;

    protected override async Task ReloadRequired()
    {
        ProjectDescriptorDto descriptor = new()
        {
            Skip = 0,
            Take = int.MaxValue,
            Search = SearchString,
            Template = TemplateMode
        };

        IReadOnlyList<ProjectDto> Items = await ProjectsService.GetListAsync(descriptor, CancellationToken.None).GetValueOrDefaultAsync() ?? [];

        projects = [.. Items];
        projects.ForEach(p => p.MetadataObject = p.GetClinicProjectMetadata());

        projectsViaSurgeryData = [.. projects.Where(project => ((ClinicProjectMetadata?)project.MetadataObject)?.DateOfSurgery.HasValue ?? false)];
        projectsLackSurgeryData = [.. projects.Where(project => !((ClinicProjectMetadata?)project.MetadataObject)?.DateOfSurgery.HasValue ?? true)];
    }
    private SortMethod[] SortMethodsViaSurgeryData => [
        new SortMethod(
            () => Localizer["SurgeryDateDesc"],
            () =>
            {
                projectsViaSurgeryData = [.. projectsViaSurgeryData!
                        .OrderByDescending(e => ((ClinicProjectMetadata?)e.MetadataObject)?.DateOfSurgery)
                        .ThenByDescending(e => (e.Stats?.NewItems ?? 0) > 0)
                        .ThenByDescending(e => e.Stats?.Unread ?? false)
                        .ThenBy(e => e.Name)];
                StateHasChanged();
            }),
        new SortMethod(
            () => Localizer["SurgeryDate"],
            () =>
            {
                projectsViaSurgeryData = [.. projectsViaSurgeryData!
                    .OrderBy(e=> ((ClinicProjectMetadata?)e.MetadataObject)?.DateOfSurgery)
                    .ThenByDescending(e => (e.Stats?.NewItems ?? 0) > 0)
                    .ThenByDescending(e => e.Stats?.Unread ?? false)
                    .ThenBy(e => e.Name)];
                StateHasChanged();
            })];

    private SortMethod[] SortMethodsLackSurgeryData =>
        [new SortMethod(
            () => Localizer["DateAscending"],
            () =>
            {
                projectsLackSurgeryData = [.. projectsLackSurgeryData!
                    .OrderByDescending(e => (e.Stats?.NewItems ?? 0) > 0)
                    .ThenByDescending(e => e.Stats?.Unread ?? false)
                    .ThenBy(e => e.Name)];
                StateHasChanged();
            })];
}
