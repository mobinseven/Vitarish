using Vitarish.Interfaces.Base.Models;
using Vitarish.Sea.Domain.Enums;

namespace Vitarish.Clinic.Interface.Components.Patients;

public partial class PatientsGrid
{
    private bool MassEditMode;
    private int patientsQueryVersion;
    private ProjectSortBy? patientsSortBy = ProjectSortBy.Name;
    private bool patientsSortDescending;

    private string PatientsQueryKey => string.Join('|', SearchString ?? string.Empty, TemplateMode, patientsSortBy?.ToString() ?? string.Empty, patientsSortDescending, patientsQueryVersion);

    protected override Task ReloadRequired()
    {
        patientsQueryVersion++;
        return InvokeAsync(StateHasChanged);
    }

    private ProjectDescriptorDto CreatePatientsDescriptor(int? skip, int? take) => new()
    {
        Skip = skip,
        Take = take,
        Search = string.IsNullOrWhiteSpace(SearchString) ? null : SearchString,
        Template = TemplateMode,
        SortBy = patientsSortBy,
        Descending = patientsSortDescending
    };

    private async Task<IReadOnlyList<PatientReadDto>> QueryPatientsAsync(int skip, int take, CancellationToken cancellationToken)
    {
        ProjectDescriptorDto descriptor = CreatePatientsDescriptor(skip, take);
        IReadOnlyList<ProjectReadDto> items = await ProjectsService.GetListAsync(descriptor, cancellationToken).GetValueOrDefaultAsync() ?? [];

        return [.. items.Select(project => project.ToPatientRead()).OfType<PatientReadDto>()];
    }

    private async Task<int> CountPatientsAsync(CancellationToken cancellationToken)
    {
        ProjectDescriptorDto descriptor = CreatePatientsDescriptor(skip: null, take: null);
        return await ProjectsService.CountAsync(descriptor, cancellationToken).GetValueOrDefaultAsync();
    }

    private Task SetPatientsSort(ProjectSortBy sortBy, bool descending)
    {
        patientsSortBy = sortBy;
        patientsSortDescending = descending;
        return ReloadRequired();
    }

    private SortMethod[] SortMethods => [
        new SortMethod(
            () => Localizer["Name"],
            () => _ = SetPatientsSort(ProjectSortBy.Name, descending: false)),
        new SortMethod(
            () => Localizer["NameDesc"],
            () => _ = SetPatientsSort(ProjectSortBy.Name, descending: true)),];

}
