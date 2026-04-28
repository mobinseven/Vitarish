using Vitarish.Interfaces.Base.Models;

namespace Vitarish.Medical.Interface.Components.Patients;

public partial class PatientsGrid
{
    private bool MassEditMode;

    private IReadOnlyList<PatientReadDto>? projectsViaSurgeryData;
    private IReadOnlyList<PatientReadDto>? projectsLackSurgeryData;

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

        List<PatientReadDto> patientProjects = [.. Items.Select(p => p.ToPatientRead()!)];
        projectsViaSurgeryData = [.. patientProjects.Where(c => c.DateOfSurgery is not null)];
        projectsLackSurgeryData = [.. patientProjects.Where(c => c.DateOfSurgery is null)];
        await InvokeAsync(StateHasChanged);
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
