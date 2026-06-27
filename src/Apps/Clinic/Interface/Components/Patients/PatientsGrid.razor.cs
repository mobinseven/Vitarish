using Vitarish.Modules.Aggregator.Interface.Models;
using SeaProjectSortBy = Vitarish.Sea.Contracts.Enums.ProjectSortBy;

namespace Vitarish.Clinic.Interface.Components.Patients;

public partial class PatientsGrid
{
    [Inject] private IClinicClient ClinicClient { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    private readonly bool MassEditMode;
    private int patientsQueryVersion;
    private PatientSortBy? patientsSortBy = PatientSortBy.Name;
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
        SortBy = ToProjectSortBy(patientsSortBy),
        Descending = patientsSortDescending
    };

    private PatientListRequest CreatePatientsQuery(int skip, int take) => new(
        Search: string.IsNullOrWhiteSpace(SearchString) ? null : SearchString,
        Skip: skip,
        Take: take,
        Template: TemplateMode,
        SortBy: patientsSortBy,
        Descending: patientsSortDescending);

    private PatientCountRequest CreatePatientsCountQuery() => new(
        Search: string.IsNullOrWhiteSpace(SearchString) ? null : SearchString,
        Template: TemplateMode);

    private async Task<IReadOnlyList<PatientViewDto>> QueryPatientsAsync(int skip, int take, CancellationToken cancellationToken)
    {
        var result = await ClinicClient.ListPatientsAsync(CreatePatientsQuery(skip, take), cancellationToken);
        if (result.Succeeded)
        {
            return result.Value ?? [];
        }

        _ = Snackbar.Add(result.Error?.Message ?? Localizer["Error"].Value, Severity.Error);
        return [];
    }

    private async Task<IReadOnlyList<ProjectReadDto>> QueryEditablePatientsAsync(int skip, int take, CancellationToken cancellationToken)
    {
        ProjectDescriptorDto descriptor = CreatePatientsDescriptor(skip, take);
        return await ProjectsService.GetListAsync(descriptor, cancellationToken).GetValueOrDefaultAsync() ?? [];
    }

    private async Task<int> CountPatientsAsync(CancellationToken cancellationToken)
    {
        var result = await ClinicClient.CountPatientsAsync(CreatePatientsCountQuery(), cancellationToken);
        return result.Succeeded ? result.Value : 0;
    }

    private static SeaProjectSortBy? ToProjectSortBy(PatientSortBy? sortBy) => sortBy switch
    {
        PatientSortBy.Name => SeaProjectSortBy.Name,
        PatientSortBy.Template => SeaProjectSortBy.Template,
        _ => null
    };

    private Task SetPatientsSort(PatientSortBy sortBy, bool descending)
    {
        patientsSortBy = sortBy;
        patientsSortDescending = descending;
        return ReloadRequired();
    }

    private SortMethod[] SortMethods => [
        new SortMethod(
            () => Localizer["Name"],
            d => _ = SetPatientsSort(PatientSortBy.Name, descending: d)),
        new SortMethod(
            () => Localizer["DateOf", Localizer["Birth"]],
            d => _ = SetPatientsSort(PatientSortBy.DateOfBirth, descending: d)),
        new SortMethod(
            () => Localizer["DateOf", Localizer["Surgery"]],
            d => _ = SetPatientsSort(PatientSortBy.DateOfSurgery, descending: d)),
        new SortMethod(
            () => Localizer["CreatedAt"],
            d => _ = SetPatientsSort(PatientSortBy.CreatedAt, descending : d)),
        new SortMethod(
            () => Localizer["UpdatedAt"],
            d => _ = SetPatientsSort(PatientSortBy.UpdatedAt, descending : d))];

}
