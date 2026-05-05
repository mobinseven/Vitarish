using MediatR;
using Vitarish.Clinical.Application.Commands;
using Vitarish.Clinical.Application.Queries;
using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Interface.Components;

public partial class ClinicalOperationsPanel : CommonBase
{
    [Inject] private IMediator Mediator { get; set; } = default!;

    private IReadOnlyList<ClinicalFhirOperationRecord> _operations = [];
    private ClinicalOperationsConfiguration? _configuration;
    private bool _loading;
    private bool _running;
    private string? _resourceType;
    private string? _resourceId;
    private string? _idempotencyKey;
    private string? _query;
    private string? _payloadJson;
    private string? _searchText;
    private FhirInteractionType _interaction = FhirInteractionType.Read;
    private FhirInteractionType? _interactionFilter;
    private FhirOperationStatus? _statusFilter;
    private string? _resultMessage;
    private FhirOperationResponse? _lastResponse;

    protected override async Task ReloadRequired()
    {
        _loading = true;

        try
        {
            _configuration = await Mediator.Send(new GetClinicalOperationsConfigurationQuery());
            _resourceType ??= _configuration.AllowedResourceTypes.FirstOrDefault();
            await LoadOperationsAsync();
        }
        finally
        {
            _loading = false;
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task LoadOperationsAsync()
    {
        _operations = await Mediator.Send(new ListFhirOperationsQuery(new ClinicalFhirOperationFilter
        {
            Interaction = _interactionFilter,
            SearchText = _searchText,
            Status = _statusFilter,
            Take = 100
        }));

        await InvokeAsync(StateHasChanged);
    }

    private async Task ExecuteAsync()
    {
        if (string.IsNullOrWhiteSpace(_resourceType))
        {
            _resultMessage = Localizer["ResourceTypeIsRequired"];
            _lastResponse = null;
            return;
        }

        _running = true;

        try
        {
            FhirOperationResponse response = await Mediator.Send(new ExecuteFhirOperationCommand(new FhirOperationRequest
            {
                CorrelationId = RequestContext.RequestId,
                IdempotencyKey = _idempotencyKey,
                Interaction = _interaction,
                PayloadJson = _payloadJson,
                Query = _query,
                ResourceId = _resourceId,
                ResourceType = _resourceType
            }));

            _lastResponse = response;
            _resultMessage = $"{response.Status} · {response.CorrelationId}";
            await LoadOperationsAsync();
        }
        catch (Exception exception)
        {
            _lastResponse = null;
            _resultMessage = exception.Message;
            await InvokeAsync(StateHasChanged);
        }
        finally
        {
            _running = false;
        }
    }

    private Task OnResourceTypeChanged(string? value)
    {
        _resourceType = value;
        return Task.CompletedTask;
    }

    private Task OnInteractionChanged(FhirInteractionType value)
    {
        _interaction = value;
        return Task.CompletedTask;
    }

    private async Task OpenOperationDetailAsync(ClinicalFhirOperationRecord operation)
    {
        DialogParameters parameters = new()
        {
            { nameof(FhirOperationDetailDialog.Operation), operation }
        };

        _ = await DialogService.ShowAsync<FhirOperationDetailDialog>(
            Localizer["OperationDetail"], parameters);
    }

    private static string FormatOperationLabel(ClinicalFhirOperationRecord operation)
    {
        string resource = string.IsNullOrWhiteSpace(operation.ResourceId)
            ? operation.ResourceType
            : $"{operation.ResourceType}/{operation.ResourceId}";
        return $"{operation.Interaction} {resource}";
    }
}

