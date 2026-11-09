using MediatR;
using Vitarish.Medical.Application.Abstractions.Prescriptions;
using Vitarish.Medical.Application.Queries.Prescriptions;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Medical.Infrastructure.Handlers.Prescriptions;

/// <summary>
/// Reads the consumption values a provider accepts for a drug's dosage form. The provider login is the
/// caller's own, resolved from the authenticated doctor and clinic and never from the request.
/// </summary>
public sealed class GetDrugConsumptionOptionsHandler(
    IInsuranceProviderRegistry providerRegistry,
    IRequestContext requestContext)
    : IRequestHandler<GetDrugConsumptionOptionsQuery, DrugConsumptionOptionsResultDto>
{
    public async Task<DrugConsumptionOptionsResultDto> Handle(GetDrugConsumptionOptionsQuery request, CancellationToken cancellationToken)
    {
        if (providerRegistry.GetAdapter(request.ProviderKey) is not IInsuranceProviderDrugConsumptionAdapter adapter)
        {
            return new DrugConsumptionOptionsResultDto { IsSupported = false, IsSuccess = true };
        }

        if (!CredentialRequestScope.TryResolve(requestContext, out Guid doctorId, out Guid clinicId))
        {
            return new DrugConsumptionOptionsResultDto { IsSupported = true, ErrorMessage = "دامنه دسترسی اعتبارنامه مشخص نیست." };
        }

        AdapterResult<DrugConsumptionOptions> result = await adapter.GetDrugConsumptionOptionsAsync(
            new DrugConsumptionOptionsRequest { DoctorId = doctorId, ClinicId = clinicId, FormCode = request.FormCode },
            cancellationToken);
        if (result.IsNotSupported)
        {
            return new DrugConsumptionOptionsResultDto { IsSupported = false, IsSuccess = true };
        }

        if (!result.IsSuccess || result.Data is null)
        {
            return new DrugConsumptionOptionsResultDto
            {
                IsSupported = true,
                ErrorMessage = result.Error?.UserMessage ?? "دریافت اطلاعات مصرف دارو ناموفق بود."
            };
        }

        DrugConsumptionOptions data = result.Data;
        return new DrugConsumptionOptionsResultDto
        {
            IsSupported = true,
            IsSuccess = true,
            Units = data.Units
                .Select(unit => new DrugConsumptionUnitDto(unit.Id, unit.Name, unit.NameLatin, unit.Amounts.Select(ToDto).ToList()))
                .ToList(),
            Instructions = data.Instructions.Select(ToDto).ToList(),
            Usages = data.Usages.Select(ToDto).ToList(),
            Methods = data.Methods.Select(ToDto).ToList()
        };
    }

    private static DrugConsumptionOptionDto ToDto(DrugConsumptionOption option) => new(option.Id, option.Name, option.NameLatin);
}
