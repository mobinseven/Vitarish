using Vitarish.Interfaces.Base.Models;
using Vitarish.Sea.Interface.Plugins.Clinic.Models;

namespace Vitarish.Sea.Interface.Plugins.Clinic.Components;

public partial class ClinicProjectsGrid
{
    private bool MassEditMode;

    private IReadOnlyList<ClinicProjectReadDto>? projectsViaSurgeryData;
    private IReadOnlyList<ClinicProjectReadDto>? projectsLackSurgeryData;

    protected override async Task ReloadRequired()
    {
        ProjectDescriptorDto descriptor = new()
        {
            Skip = 0,
            Take = int.MaxValue,
            Search = string.IsNullOrWhiteSpace(SearchString) ? null : SearchString,
            Template = TemplateMode
        };

        IReadOnlyList<ProjectReadDto> Items = await ProjectsService.GetListAsync(descriptor, CancellationToken.None).GetValueOrDefaultAsync() ?? [];

        projects = [.. Items];
        List<ClinicProjectReadDto> clinicProjects = [.. Items.Select(p => ClinicProjectReadDto.ToRead(p)!)];
        projectsViaSurgeryData = [.. clinicProjects.Where(c => c.DateOfSurgery is not null)];
        projectsLackSurgeryData = [.. clinicProjects.Where(c => c.DateOfSurgery is null)];
        StateHasChanged();
    }
    private SortMethod[] SortMethodsViaSurgeryData => [
        new SortMethod(
            () => Localizer["SurgeryDateDesc"],
            () =>
            {
                projectsViaSurgeryData = [.. projectsViaSurgeryData!
                        .OrderByDescending(e => e.DateOfSurgery)
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
                    .OrderBy(e=> e.DateOfSurgery)
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
