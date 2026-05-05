using MediatR;
using MudBlazor;
using Vitarish.Clinical.Application.Commands;
using Vitarish.Clinical.Application.Queries;
using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Interface.Components;

public partial class ClinicalProjectionsPanel : CommonBase
{
    [Inject] private IMediator Mediator { get; set; } = default!;

    private IReadOnlyList<ProjectionReadDto> _projections = [];
    private bool _loading;
    private bool _syncing;
    private string? _errorMessage;
    private string? _syncResult;
    private Severity _syncSeverity;

    private string? _resourceType;
    private string? _resourceId;
    private string? _patientReference;
    private bool _includeExpired;

    private string? _syncResourceType;
    private string? _syncResourceId;

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

    private async Task SyncAsync(string resourceType, string resourceId)
    {
        _syncing = true;
        _syncResult = null;

        try
        {
            ProjectionReadDto result = await Mediator.Send(new SyncProjectionCommand(resourceType, resourceId));
            _syncResult = $"{Localizer["SyncedSuccessfully"]}: {result.DisplayName}";
            _syncSeverity = Severity.Success;
            await LoadAsync();
        }
        catch (Exception exception)
        {
            _syncResult = exception.Message;
            _syncSeverity = Severity.Error;
        }
        finally
        {
            _syncing = false;
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task SyncNewAsync()
    {
        if (string.IsNullOrWhiteSpace(_syncResourceType) || string.IsNullOrWhiteSpace(_syncResourceId))
        {
            _syncResult = Localizer["ResourceTypeAndIdRequired"];
            _syncSeverity = Severity.Warning;
            return;
        }

        await SyncAsync(_syncResourceType, _syncResourceId);
    }

    private static Color GetStateColor(FhirOperationState state) => state switch
    {
        FhirOperationState.Succeeded => Color.Success,
        FhirOperationState.Failed => Color.Error,
        FhirOperationState.Pending => Color.Warning,
        _ => Color.Default
    };
}
