using MediatR;
using Vitarish.Clinical.Application.Commands;
using Vitarish.Clinical.Application.Queries;
using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Interface.Components;

public partial class ClinicalProjectionsPanel : CommonBase
{
    [Inject] private IMediator Mediator { get; set; } = default!;

    private IReadOnlyList<ProjectionReadDto> _projections = [];
    private bool _loading;
    private string? _errorMessage;

    private string? _resourceType;
    private string? _resourceId;
    private string? _patientReference;
    private bool _includeExpired;

    protected override Task ReloadRequired() => LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true;
        _errorMessage = null;

        try
        {
            _projections = await Mediator.Send(new ListProjectionsQuery(new ProjectionFilter
            {
                ResourceType = _resourceType,
                ResourceId = _resourceId,
                PatientReference = _patientReference,
                IncludeExpired = _includeExpired,
                Take = 100
            }));
        }
        catch (Exception exception)
        {
            _errorMessage = exception.Message;
        }
        finally
        {
            _loading = false;
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task OpenSyncDialogAsync()
    {
        IDialogReference dialog = await DialogService.ShowAsync<SyncFhirProjectionDialog>(Localizer["SyncResource"]);
        DialogResult? result = await dialog.Result;

        if (result is not null && !result.Canceled)
        {
            await LoadAsync();
        }
    }

    private async Task OpenProjectionDetailAsync(ProjectionReadDto projection)
    {
        DialogParameters parameters = new()
        {
            { nameof(FhirProjectionDetailDialog.Projection), projection }
        };

        IDialogReference dialog = await DialogService.ShowAsync<FhirProjectionDetailDialog>(
            Localizer["FhirProjection"], parameters);
        DialogResult? result = await dialog.Result;

        if (result is not null && !result.Canceled)
        {
            await LoadAsync();
        }
    }

    private static string FormatProjectionHelperText(ProjectionReadDto proj)
    {
        string displayPart = string.IsNullOrWhiteSpace(proj.DisplayName) ? "" : proj.DisplayName;
        string datePart = proj.LastSyncedAt.HasValue
            ? proj.LastSyncedAt.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm")
            : "";
        return string.Join(" · ", new[] { displayPart, datePart }.Where(s => !string.IsNullOrWhiteSpace(s)));
    }
}
