using MediatR;
using Vitarish.Medical.Application.Commands;
using Vitarish.Prescription.Application.Abstractions;
using Vitarish.Prescription.Application.Commands;
using Vitarish.Prescription.Domain.Enums;

namespace Vitarish.Prescription.Infrastructure.Handlers;

/// <summary>
/// Handles SyncReferenceDataCommand by fetching reference data from the insurance provider
/// adapter and delegating persistence to Medical's SyncProviderCodesCommand.
/// </summary>
public sealed class SyncReferenceDataHandler(
    IInsuranceProviderRegistry providerRegistry,
    IMediator mediator)
    : IRequestHandler<SyncReferenceDataCommand, SyncReferenceDataResult>
{
    public async Task<SyncReferenceDataResult> Handle(
        SyncReferenceDataCommand request,
        CancellationToken cancellationToken)
    {
        IInsuranceProviderAdapter? adapter = providerRegistry.GetAdapter(request.ProviderKey);
        if (adapter is null)
            return SyncReferenceDataResult.Failure($"Provider '{request.ProviderKey}' not found");

        var capabilities = adapter.GetCapabilities();
        bool supportsDrug = capabilities.Contains(InsuranceProviderCapabilityType.SupportsReferenceDrugSync);
        bool supportsService = capabilities.Contains(InsuranceProviderCapabilityType.SupportsReferenceServiceSync);

        if (!supportsDrug && !supportsService)
            return SyncReferenceDataResult.Failure("Provider does not support reference data sync");

        if (request.CodeSystem.Contains("drug", StringComparison.OrdinalIgnoreCase) && !supportsDrug)
            return SyncReferenceDataResult.Failure("Provider does not support drug code sync");

        if (request.CodeSystem.Contains("service", StringComparison.OrdinalIgnoreCase) && !supportsService)
            return SyncReferenceDataResult.Failure("Provider does not support service code sync");

        var fetchRequest = new ReferenceDataRequest
        {
            CodeSystem = request.CodeSystem
        };

        AdapterResult<ReferenceDataResponse> fetchResult =
            await adapter.FetchReferenceDataAsync(fetchRequest, cancellationToken);

        if (!fetchResult.IsSuccess || fetchResult.Data is null)
            return SyncReferenceDataResult.Failure(fetchResult.Error?.UserMessage ?? "Failed to fetch reference data from provider");

        ReferenceDataResponse response = fetchResult.Data;

        IReadOnlyList<SyncProviderCodeItem> codes = response.Items
            .Select(i => new SyncProviderCodeItem(i.Code, i.DisplayName, i.Category, i.IsActive))
            .ToList();

        var syncCommand = new SyncProviderCodesCommand
        {
            ProviderKey = request.ProviderKey,
            CodeSystem = request.CodeSystem,
            DataVersion = response.Version,
            Codes = codes
        };

        var importResult = await mediator.Send(syncCommand, cancellationToken);

        if (!importResult.IsSuccess)
            return SyncReferenceDataResult.Failure(importResult.Errors?.FirstOrDefault()?.Message ?? "Import failed");

        return SyncReferenceDataResult.Success(importResult.Value!.ImportBatchId, importResult.Value.RecordsProcessed);
    }
}
