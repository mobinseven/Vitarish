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
    private ClinicalFhirOperationRecord? _selectedOperation;
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
    private Severity _resultSeverity = Severity.Info;

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

        _selectedOperation ??= _operations.FirstOrDefault();
        await InvokeAsync(StateHasChanged);
    }

    private async Task ExecuteAsync()
    {
        if (string.IsNullOrWhiteSpace(_resourceType))
        {
            _resultSeverity = Severity.Warning;
            _resultMessage = Localizer["ResourceTypeIsRequired"];
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

            _resultSeverity = response.Status == FhirOperationStatus.Succeeded ? Severity.Success : response.Status == FhirOperationStatus.Denied ? Severity.Warning : Severity.Error;
            _resultMessage = $"{response.Status} · {response.CorrelationId}";
            await LoadOperationsAsync();
            _selectedOperation = _operations.FirstOrDefault(operation => operation.Id == response.OperationId) ?? _operations.FirstOrDefault();
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

    private void SelectOperation(ClinicalFhirOperationRecord operation) => _selectedOperation = operation;

    private static string FormatResource(ClinicalFhirOperationRecord operation) =>
        string.IsNullOrWhiteSpace(operation.ResourceId) ? operation.ResourceType : $"{operation.ResourceType}/{operation.ResourceId}";

    private static Color GetStatusColor(FhirOperationStatus status) => status switch
    {
        FhirOperationStatus.Succeeded => Color.Success,
        FhirOperationStatus.Denied => Color.Warning,
        FhirOperationStatus.Failed => Color.Error,
        FhirOperationStatus.Running => Color.Info,
        _ => Color.Default
    };
}
