using System.Runtime.CompilerServices;
using Tamin.Integration;
using Tamin.Integration.Mapping;

namespace Vitarish.Medical.Infrastructure.Providers.Prescriptions.Tamin;

internal sealed class TaminMedicalInfrastructureGateway : ITaminMedicalInfrastructureGateway
{
    public async Task<TaminReferencePageResult> SearchDrugsAsync(
        ITaminClient client,
        TaminDrugSearchOperationRequest request,
        CancellationToken cancellationToken)
    {
        UnconstrainedResponse response = await client.ListServicesAsync(TaminServiceTypeCodes.Drug, cancellationToken);
        return TaminProviderJsonReader.ToReferencePage(
            TaminProviderOperationNames.Drug,
            response.Payload,
            request.Page,
            request.PageSize);
    }

    public async IAsyncEnumerable<TaminReferencePageResult> ListAllDrugPagesAsync(
        ITaminClient client,
        TaminDrugListAllOperationRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return await SearchDrugsAsync(
            client,
            new TaminDrugSearchOperationRequest(
                request.SearchText,
                request.DrugCode,
                Page: 1,
                request.PageSize,
                request.ActiveOnly),
            cancellationToken);
    }

    public async Task<TaminReferencePageResult> SearchServicesAsync(
        ITaminClient client,
        TaminServiceSearchOperationRequest request,
        CancellationToken cancellationToken)
    {
        UnconstrainedResponse response = await client.ListServicesAsync(
            string.IsNullOrWhiteSpace(request.ServiceType) ? TaminServiceTypeCodes.Laboratory : request.ServiceType,
            cancellationToken);
        return TaminProviderJsonReader.ToReferencePage(
            TaminProviderOperationNames.Service,
            response.Payload,
            request.Page,
            request.PageSize);
    }

    public async IAsyncEnumerable<TaminReferencePageResult> ListAllServicePagesAsync(
        ITaminClient client,
        TaminServiceListAllOperationRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return await SearchServicesAsync(
            client,
            new TaminServiceSearchOperationRequest(
                request.ServiceType,
                request.ServiceGroup,
                request.SearchText,
                Page: 1,
                request.PageSize,
                request.ActiveOnly),
            cancellationToken);
    }

    public async Task<TaminReferencePageResult> GetAllServicesAsync(
        ITaminClient client,
        TaminRawReferenceQueryOperationRequest request,
        CancellationToken cancellationToken)
    {
        request.Query.TryGetValue(TaminProviderOperationNames.ServiceTypeQuery, out string? serviceType);
        UnconstrainedResponse response = await client.ListServicesAsync(
            string.IsNullOrWhiteSpace(serviceType) ? TaminServiceTypeCodes.Laboratory : serviceType,
            cancellationToken);
        return TaminProviderJsonReader.ToReferencePage(
            TaminProviderOperationNames.AllServices,
            response.Payload,
            null,
            null);
    }

    public Task<TaminReferencePageResult> GetPrescriptionTypesAsync(
        ITaminClient client,
        CancellationToken cancellationToken) =>
        ReadReferenceAsync(TaminProviderOperationNames.PrescriptionType, client.ListPrescriptionTypesAsync, cancellationToken);

    public Task<TaminReferencePageResult> GetParaclinicTariffsAsync(
        ITaminClient client,
        CancellationToken cancellationToken) =>
        ReadReferenceAsync(TaminProviderOperationNames.ParaclinicTariff, client.ListLaboratoryTariffGroupsAsync, cancellationToken);

    public Task<TaminReferencePageResult> GetDrugAmountsAsync(
        ITaminClient client,
        CancellationToken cancellationToken) =>
        ReadReferenceAsync(TaminProviderOperationNames.DrugAmount, client.ListDrugAmountsAsync, cancellationToken);

    public Task<TaminReferencePageResult> GetDrugInstructionsAsync(
        ITaminClient client,
        CancellationToken cancellationToken) =>
        ReadReferenceAsync(TaminProviderOperationNames.DrugInstruction, client.ListDrugInstructionsAsync, cancellationToken);

    public Task<TaminReferencePageResult> GetDrugUsagesAsync(
        ITaminClient client,
        CancellationToken cancellationToken) =>
        ReadReferenceAsync(TaminProviderOperationNames.DrugUsage, client.ListDrugUsagesAsync, cancellationToken);

    public Task<TaminReferencePageResult> GetDosageFormsAsync(
        ITaminClient client,
        CancellationToken cancellationToken) =>
        ReadReferenceAsync(TaminProviderOperationNames.DosageForm, client.ListDosageFormsAsync, cancellationToken);

    private static async Task<TaminReferencePageResult> ReadReferenceAsync(
        string operationName,
        Func<CancellationToken, Task<UnconstrainedResponse>> call,
        CancellationToken cancellationToken)
    {
        UnconstrainedResponse response = await call(cancellationToken);
        return TaminProviderJsonReader.ToReferencePage(operationName, response.Payload, null, null);
    }
}
