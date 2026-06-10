using MediatR;
using Vitarish.Clinic.Application.Queries.Patients;
using Vitarish.Interfaces.Base.Models;
using Vitarish.Sea.Domain.Enums;

namespace Vitarish.Clinic.Interface.Components.Patients;

public partial class PatientsGrid
{
    [Inject] private ISender Sender { get; set; } = default!;

    private readonly bool MassEditMode;
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

    private GetPatientsQuery CreatePatientsQuery(int skip, int take) => new(
        Search: string.IsNullOrWhiteSpace(SearchString) ? null : SearchString,
        Skip: skip,
        Take: take,
        Template: TemplateMode,
        SortBy: patientsSortBy,
        Descending: patientsSortDescending);

    private CountPatientsQuery CreatePatientsCountQuery() => new(
        Search: string.IsNullOrWhiteSpace(SearchString) ? null : SearchString,
        Template: TemplateMode);

    private Task<IReadOnlyList<PatientView>> QueryPatientsAsync(int skip, int take, CancellationToken cancellationToken)
        => Sender.Send(CreatePatientsQuery(skip, take), cancellationToken);

    private async Task<IReadOnlyList<ProjectReadDto>> QueryEditablePatientsAsync(int skip, int take, CancellationToken cancellationToken)
    {
        ProjectDescriptorDto descriptor = CreatePatientsDescriptor(skip, take);
        return await ProjectsService.GetListAsync(descriptor, cancellationToken).GetValueOrDefaultAsync() ?? [];
    }

    private Task<int> CountPatientsAsync(CancellationToken cancellationToken)
        => Sender.Send(CreatePatientsCountQuery(), cancellationToken);

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
