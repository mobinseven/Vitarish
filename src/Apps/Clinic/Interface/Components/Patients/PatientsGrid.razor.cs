using Vitarish.Clinic.Domain.Models;
using Vitarish.Interfaces.Base.Models;

namespace Vitarish.Clinic.Interface.Components.Patients;

public partial class PatientsGrid
{
    private bool MassEditMode;

    private IReadOnlyList<PatientReadDto>? patients;
    ProjectDescriptorDto descriptor = new();
    protected override async Task ReloadRequired()
    {
        ProjectDescriptorDto descriptor = new()
        {
            Skip = 0,
            Take = 10,
            Search = string.IsNullOrWhiteSpace(SearchString) ? null : SearchString,
            Template = TemplateMode
        };

        IReadOnlyList<ProjectReadDto> Items = await ProjectsService.GetListAsync(descriptor, CancellationToken.None).GetValueOrDefaultAsync() ?? [];

        patients = [.. Items.Select(p => p.ToPatientRead()!)];
        await InvokeAsync(StateHasChanged);
    }

    private SortMethod[] SortMethods => [
        new SortMethod(
            () => Localizer["SurgeryDateDesc"],
            () =>
            {
                patients = [.. patients!
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
                patients = [.. patients!
                    .OrderBy(e=> e.DateOfSurgery)
                    .ThenByDescending(e => (e.Stats?.NewItems ?? 0) > 0)
                    .ThenByDescending(e => e.Stats?.Unread ?? false)
                    .ThenBy(e => e.Name)];
                StateHasChanged();
            }),
        new SortMethod(
            () => Localizer["Name"],
            () =>
            {
                patients = [.. patients!
                        .OrderBy(e => e.Name)
                        .ThenByDescending(e => (e.Stats?.NewItems ?? 0) > 0)
                        .ThenByDescending(e => e.Stats?.Unread ?? false)];
                StateHasChanged();
            }),
        new SortMethod(
            () => Localizer["NameDesc"],
            () =>
            {
                patients = [.. patients!
                        .OrderByDescending(e => e.Name)
                        .ThenByDescending(e => (e.Stats?.NewItems ?? 0) > 0)
                        .ThenByDescending(e => e.Stats?.Unread ?? false)];
                StateHasChanged();
            }),];

}
