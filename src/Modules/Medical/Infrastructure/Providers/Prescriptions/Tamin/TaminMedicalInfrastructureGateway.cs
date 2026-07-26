using System.Runtime.CompilerServices;
using System.Text.Json;
using Tamin.Integration.Mapping;

namespace Vitarish.Medical.Infrastructure.Providers.Prescriptions.Tamin;

internal sealed class TaminMedicalInfrastructureGateway(
    TaminProviderSessionFactory sessionFactory,
    TaminMappingService mappingService) : ITaminMedicalInfrastructureGateway
{
    private readonly TaminProviderSessionFactory _sessionFactory = sessionFactory;
    private readonly TaminMappingService _mappingService = mappingService;

    public async Task<TaminReferencePageResult> SearchDrugsAsync(
        TaminDrugSearchOperationRequest request,
        CancellationToken cancellationToken)
    {
        JsonElement payload = await GetServicesAsync("01", cancellationToken);
        return TaminProviderJsonReader.ToReferencePage(
            TaminProviderOperationNames.Drug,
            payload,
            request.Page,
            request.PageSize);
    }

    public async IAsyncEnumerable<TaminReferencePageResult> ListAllDrugPagesAsync(
        TaminDrugListAllOperationRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return await SearchDrugsAsync(
            new TaminDrugSearchOperationRequest(
                request.SearchText,
                request.DrugCode,
                Page: 1,
                request.PageSize,
                request.ActiveOnly),
            cancellationToken);
    }

    public async Task<TaminReferencePageResult> SearchServicesAsync(
        TaminServiceSearchOperationRequest request,
        CancellationToken cancellationToken)
    {
        JsonElement payload = await GetServicesAsync(
            string.IsNullOrWhiteSpace(request.ServiceType) ? "02" : request.ServiceType,
            cancellationToken);
        return TaminProviderJsonReader.ToReferencePage(
            TaminProviderOperationNames.Service,
            payload,
            request.Page,
            request.PageSize);
    }

    public async IAsyncEnumerable<TaminReferencePageResult> ListAllServicePagesAsync(
        TaminServiceListAllOperationRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return await SearchServicesAsync(
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
        TaminRawReferenceQueryOperationRequest request,
        CancellationToken cancellationToken)
    {
        request.Query.TryGetValue("service-type", out string? serviceType);
        JsonElement payload = await GetServicesAsync(
            string.IsNullOrWhiteSpace(serviceType) ? "02" : serviceType,
            cancellationToken);
        return TaminProviderJsonReader.ToReferencePage(
            TaminProviderOperationNames.AllServices,
            payload,
            null,
            null);
    }

    public async Task<TaminReferencePageResult> GetPrescriptionTypesAsync(
        TaminRawReferenceQueryOperationRequest request,
        CancellationToken cancellationToken)
    {
        using TaminProviderSession session = _sessionFactory.CreateAnonymous();
        JsonElement payload;
        if (session.Production is { } production)
        {
            var response = await production.Soa.Interface.Epresc.SendEpresc.V2.PrescriptionType
                .GetAsync(cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("Tamin prescription-type response was empty.");
            payload = _mappingService.MapPrescriptionTypes(response).Payload;
        }
        else
        {
            var response = await session.Pilot!.Soa.Api.V2.WsPrescriptionType
                .GetAsync(cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("Tamin pilot prescription-type response was empty.");
            payload = _mappingService.MapPilotReferenceData(response).Payload;
        }

        return TaminProviderJsonReader.ToReferencePage(
            TaminProviderOperationNames.PrescriptionType,
            payload,
            null,
            null);
    }

    public Task<TaminReferencePageResult> GetParaclinicTariffsAsync(
        TaminRawReferenceQueryOperationRequest request,
        CancellationToken cancellationToken) =>
        GetSimpleReferenceAsync(
            TaminProviderOperationNames.ParaclinicTariff,
            static (session, ct) => session.Production!.Soa.Interface.Epresc.SendEpresc.V2.ParTaref.GetAsync(cancellationToken: ct),
            static (session, ct) => session.Pilot!.Soa.Api.V2.WsParTaref.GetAsync(cancellationToken: ct),
            cancellationToken);

    public Task<TaminReferencePageResult> GetDrugAmountsAsync(
        TaminRawReferenceQueryOperationRequest request,
        CancellationToken cancellationToken) =>
        GetSimpleReferenceAsync(
            TaminProviderOperationNames.DrugAmount,
            static (session, ct) => session.Production!.Soa.Interface.Epresc.SendEpresc.V2.DrugAmount.GetAsync(cancellationToken: ct),
            static (session, ct) => session.Pilot!.Soa.Api.V2.WsDrugAmount.GetAsync(cancellationToken: ct),
            cancellationToken);

    public Task<TaminReferencePageResult> GetDrugInstructionsAsync(
        TaminRawReferenceQueryOperationRequest request,
        CancellationToken cancellationToken) =>
        GetSimpleReferenceAsync(
            TaminProviderOperationNames.DrugInstruction,
            static (session, ct) => session.Production!.Soa.Interface.Epresc.SendEpresc.V2.DrugInstruction.GetAsync(cancellationToken: ct),
            static (session, ct) => session.Pilot!.Soa.Api.V2.WsDrugInstruction.GetAsync(cancellationToken: ct),
            cancellationToken);

    private async Task<JsonElement> GetServicesAsync(string serviceType, CancellationToken cancellationToken)
    {
        using TaminProviderSession session = _sessionFactory.CreateAnonymous();
        if (session.Production is { } production)
        {
            var response = await production.Soa.Interface.Epresc.SendEpresc.V2.Services.GetAsync(
                configuration => configuration.QueryParameters.ServiceType = serviceType,
                cancellationToken)
                ?? throw new InvalidOperationException("Tamin services response was empty.");
            return _mappingService.MapServices(response).Payload;
        }

        var pilotResponse = await session.Pilot!.Soa.Api.V2.WsServices.GetAsync(
            configuration => configuration.QueryParameters.ServiceType = serviceType,
            cancellationToken)
            ?? throw new InvalidOperationException("Tamin pilot services response was empty.");
        return _mappingService.MapPilotReferenceData(pilotResponse).Payload;
    }

    private async Task<TaminReferencePageResult> GetSimpleReferenceAsync(
        string operationName,
        Func<TaminProviderSession, CancellationToken, Task<global::Tamin.Client.Soa.Prod.Models.JsonObject?>> productionCall,
        Func<TaminProviderSession, CancellationToken, Task<global::Tamin.Client.Soa.Pilot.Models.JsonObject?>> pilotCall,
        CancellationToken cancellationToken)
    {
        using TaminProviderSession session = _sessionFactory.CreateAnonymous();
        JsonElement payload;
        if (session.Production is not null)
        {
            var response = await productionCall(session, cancellationToken)
                ?? throw new InvalidOperationException($"Tamin {operationName} response was empty.");
            payload = operationName switch
            {
                TaminProviderOperationNames.ParaclinicTariff => _mappingService.MapLaboratoryTariffGroups(response).Payload,
                TaminProviderOperationNames.DrugAmount => _mappingService.MapDrugAmounts(response).Payload,
                TaminProviderOperationNames.DrugInstruction => _mappingService.MapDrugInstructions(response).Payload,
                _ => throw new NotSupportedException($"Tamin reference operation '{operationName}' is not supported.")
            };
        }
        else
        {
            var response = await pilotCall(session, cancellationToken)
                ?? throw new InvalidOperationException($"Tamin pilot {operationName} response was empty.");
            payload = _mappingService.MapPilotReferenceData(response).Payload;
        }

        return TaminProviderJsonReader.ToReferencePage(operationName, payload, null, null);
    }
}
