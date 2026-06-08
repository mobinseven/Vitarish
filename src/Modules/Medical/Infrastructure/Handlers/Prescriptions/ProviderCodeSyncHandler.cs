using MediatR;
using Vitarish.Entities.Server.Models;
using Vitarish.Medical.Application.Commands;
using Vitarish.Medical.Application.Contracts;
using Vitarish.Medical.Application.Abstractions;
using Vitarish.Medical.Application.Commands;
using Vitarish.Medical.Domain.Enums;

namespace Vitarish.Medical.Infrastructure.Handlers.Prescriptions;

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
        {
            return SyncReferenceDataResult.Failure($"Provider '{request.ProviderKey}' not found");
        }

        IReadOnlySet<InsuranceProviderCapabilityType> capabilities = adapter.GetCapabilities();
        bool supportsDrug = capabilities.Contains(InsuranceProviderCapabilityType.SupportsReferenceDrugSync);
        bool supportsService = capabilities.Contains(InsuranceProviderCapabilityType.SupportsReferenceServiceSync);

        if (!supportsDrug && !supportsService)
        {
            return SyncReferenceDataResult.Failure("Provider does not support reference data sync");
        }

        if (request.CodeSystem.Contains("drug", StringComparison.OrdinalIgnoreCase) && !supportsDrug)
        {
            return SyncReferenceDataResult.Failure("Provider does not support drug code sync");
        }

        if (request.CodeSystem.Contains("service", StringComparison.OrdinalIgnoreCase) && !supportsService)
        {
            return SyncReferenceDataResult.Failure("Provider does not support service code sync");
        }

        ReferenceDataRequest fetchRequest = new()
        {
            CodeSystem = request.CodeSystem
        };

        AdapterResult<ReferenceDataResponse> fetchResult =
            await adapter.FetchReferenceDataAsync(fetchRequest, cancellationToken);

        if (!fetchResult.IsSuccess || fetchResult.Data is null)
        {
            return SyncReferenceDataResult.Failure(fetchResult.Error?.UserMessage ?? "Failed to fetch reference data from provider");
        }

        ReferenceDataResponse response = fetchResult.Data;

        IReadOnlyList<SyncProviderCodeItem> codes = response.Items
            .Select(i => new SyncProviderCodeItem(i.Code, i.DisplayName, i.Category, i.IsActive))
            .ToList();

        SyncProviderCodesCommand syncCommand = new()
        {
            ProviderKey = request.ProviderKey,
            CodeSystem = request.CodeSystem,
            DataVersion = response.Version,
            Codes = codes
        };

        EntityResult<ImportResult> importResult = await mediator.Send(syncCommand, cancellationToken);

        return !importResult.IsSuccess
            ? SyncReferenceDataResult.Failure(importResult.Errors?.FirstOrDefault()?.Message ?? "Import failed")
            : SyncReferenceDataResult.Success(importResult.Value!.ImportBatchId, importResult.Value.RecordsProcessed);
    }
}
